using DisputePortal.Api.Data;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DisputePortal.Api.Services;

public class DisputeService : IDisputeService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DisputeService> _logger;

    public DisputeService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService,
        ILogger<DisputeService> logger)
    {
        _db = db;
        _userManager = userManager;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<DisputeResponse> CreateDisputeAsync(
        string customerId,
        CreateDisputeRequest request)
    {
        var transaction = await _db.Transactions
            .Include(x => x.Account)
            .Include(x => x.Dispute)
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId);

        if (transaction is null)
        {
            throw new KeyNotFoundException("Transaction not found.");
        }

        if (transaction.Account.UserId != customerId)
        {
            throw new UnauthorizedAccessException(
                "You do not have access to this transaction."
            );
        }

        if (!transaction.IsDisputable)
        {
            throw new InvalidOperationException(
                "This transaction is not eligible for dispute."
            );
        }

        if (transaction.Dispute is not null)
        {
            throw new InvalidOperationException(
                "This transaction already has an active dispute."
            );
        }

        var dispute = new Dispute
        {
            CaseNumber = $"DSP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            TransactionId = transaction.Id,
            CustomerId = customerId,
            Reason = request.Reason,
            CustomerNotes = request.CustomerNotes,
            Status = DisputeStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        dispute.Events.Add(new DisputeEvent
        {
            Status = DisputeStatus.Submitted,
            Message = "Dispute submitted by customer.",
            CreatedBy = "Customer",
            CreatedAt = DateTime.UtcNow
        });

        _db.Disputes.Add(dispute);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Dispute {CaseNumber} created for customer {CustomerId}",
            dispute.CaseNumber, customerId);

        var customer = await _userManager.FindByIdAsync(customerId);

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            if (admin.Email is not null)
            {
                await _notificationService.LogNotificationAsync(
                    admin.Email,
                    "New Dispute Submitted",
                    $"Dispute {dispute.CaseNumber} submitted by {customer?.FullName ?? customer?.Email} for {dispute.Transaction.MerchantName} (R {dispute.Transaction.Amount:F2})."
                );
            }
        }

        await _notificationService.PushDisputeEventAsync("role:Admin", "DisputeCreated");

        return await GetDisputeByIdAsync(dispute.Id, customerId, false);
    }

    public async Task<List<TransactionResponse>> GetCustomerTransactionsAsync(
        string customerId)
    {
        return await _db.Transactions
            .Include(x => x.Account)
            .Include(x => x.Dispute)
            .Where(x => x.Account.UserId == customerId)
            .OrderByDescending(x => x.TransactionDate)
            .Select(x => new TransactionResponse(
                x.Id,
                x.Reference,
                x.MerchantName,
                x.Amount,
                x.Type.ToString(),
                x.TransactionDate,
                x.Description,
                x.IsDisputable,
                x.Dispute != null
            ))
            .ToListAsync();
    }

    public async Task<List<DisputeResponse>> GetCustomerDisputesAsync(
        string customerId)
    {
        var disputes = await _db.Disputes
            .Include(x => x.Transaction)
            .Include(x => x.Events)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return disputes.Select(d => MapDispute(d)).ToList();
    }

    public async Task<PagedResult<DisputeResponse>> GetAllDisputesForAdminAsync(
        int page,
        int pageSize,
        DisputeStatus? status = null,
        string? search = null)
    {
        var query = _db.Disputes
            .Include(x => x.Transaction)
            .Include(x => x.Events)
            .Include(x => x.Customer)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.CaseNumber.ToLower().Contains(term) ||
                x.Customer.FullName.ToLower().Contains(term) ||
                x.Customer.Email!.ToLower().Contains(term));
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var total = await query.CountAsync();

        var disputes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DisputeResponse>(
            disputes.Select(d => MapDispute(d, d.Customer.FullName)).ToList(),
            total,
            page,
            pageSize
        );
    }

    public async Task<DisputeResponse> GetDisputeByIdAsync(
        int disputeId,
        string userId,
        bool isAdmin)
    {
        var dispute = await _db.Disputes
            .Include(x => x.Transaction)
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == disputeId);

        if (dispute is null)
        {
            throw new KeyNotFoundException("Dispute not found.");
        }

        if (!isAdmin && dispute.CustomerId != userId)
        {
            throw new UnauthorizedAccessException(
                "You do not have access to this dispute."
            );
        }

        return MapDispute(dispute);
    }

    public async Task<DisputeResponse> UpdateDisputeStatusAsync(
        int disputeId,
        UpdateDisputeStatusRequest request,
        string adminName)
    {
        var dispute = await _db.Disputes
            .Include(x => x.Customer)
            .Include(x => x.Transaction)
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == disputeId);

        if (dispute is null)
        {
            throw new KeyNotFoundException("Dispute not found.");
        }

        if (!IsValidStatusTransition(dispute.Status, request.Status))
        {
            throw new InvalidOperationException(
                $"Invalid status transition from {dispute.Status} to {request.Status}."
            );
        }

        dispute.Status = request.Status;
        dispute.AdminNotes = request.AdminNotes;

        if (request.Status is DisputeStatus.Approved
            or DisputeStatus.Rejected
            or DisputeStatus.Resolved)
        {
            dispute.ResolvedAt = DateTime.UtcNow;
        }

        dispute.Events.Add(new DisputeEvent
        {
            Status = request.Status,
            Message = request.AdminNotes
                ?? $"Dispute status updated to {request.Status}.",
            CreatedBy = adminName,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Dispute {CaseNumber} updated to {Status} by {AdminName}",
            dispute.CaseNumber, request.Status, adminName);

        if (dispute.Customer.Email is not null)
        {
            await _notificationService.LogNotificationAsync(
                dispute.Customer.Email,
                "Dispute Status Updated",
                $"Your dispute {dispute.CaseNumber} is now {dispute.Status}."
            );
            await _notificationService.PushDisputeEventAsync(dispute.Customer.Email, "DisputeUpdated");
        }

        await _notificationService.PushDisputeEventAsync("role:Admin", "DisputeUpdated");

        return MapDispute(dispute);
    }

    private static bool IsValidStatusTransition(
        DisputeStatus currentStatus,
        DisputeStatus newStatus)
    {
        return currentStatus switch
        {
            DisputeStatus.Submitted =>
                newStatus is DisputeStatus.UnderReview
                    or DisputeStatus.MoreInfoRequired
                    or DisputeStatus.Rejected,

            DisputeStatus.UnderReview =>
                newStatus is DisputeStatus.MoreInfoRequired
                    or DisputeStatus.Approved
                    or DisputeStatus.Rejected
                    or DisputeStatus.Resolved,

            DisputeStatus.MoreInfoRequired =>
                newStatus is DisputeStatus.UnderReview
                    or DisputeStatus.Rejected,

            DisputeStatus.Approved =>
                newStatus is DisputeStatus.Resolved,

            DisputeStatus.Rejected => false,

            DisputeStatus.Resolved => false,

            _ => false
        };
    }

    private static DisputeResponse MapDispute(Dispute dispute, string? customerName = null)
    {
        return new DisputeResponse(
            dispute.Id,
            dispute.CaseNumber,
            dispute.TransactionId,
            dispute.Transaction.MerchantName,
            dispute.Transaction.Amount,
            dispute.Reason,
            dispute.CustomerNotes,
            dispute.AdminNotes,
            dispute.Status.ToString(),
            dispute.CreatedAt,
            dispute.ResolvedAt,
            dispute.Events
                .OrderBy(x => x.CreatedAt)
                .Select(e => new DisputeEventResponse(
                    e.Status.ToString(),
                    e.Message,
                    e.CreatedBy,
                    e.CreatedAt
                ))
                .ToList(),
            customerName
        );
    }
}

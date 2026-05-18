using DisputePortal.Api.Data;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Exceptions;
using DisputePortal.Api.Helpers;
using DisputePortal.Api.Models;
using Microsoft.AspNetCore.Http;
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
            throw new BusinessRuleException(
                "This transaction is not eligible for dispute."
            );
        }

        if (transaction.Dispute is not null)
        {
            throw new BusinessRuleException(
                "This transaction already has an active dispute."
            );
        }

        var dispute = new Dispute
        {
            CaseNumber = $"DSP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            TransactionId = transaction.Id,
            CustomerId = customerId,
            Reason = InputSanitizer.Sanitize(request.Reason),
            CustomerNotes = InputSanitizer.Sanitize(request.CustomerNotes),
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
            .Include(x => x.Attachments)
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
            .Include(x => x.Attachments)
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
            .Include(x => x.Attachments)
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
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == disputeId);

        if (dispute is null)
        {
            throw new KeyNotFoundException("Dispute not found.");
        }

        if (!IsValidStatusTransition(dispute.Status, request.Status))
        {
            throw new BusinessRuleException(
                $"Invalid status transition from {dispute.Status} to {request.Status}."
            );
        }

        dispute.Status = request.Status;
        dispute.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes)
            ? null
            : InputSanitizer.Sanitize(request.AdminNotes);

        if (request.Status is DisputeStatus.Approved
            or DisputeStatus.Rejected
            or DisputeStatus.Resolved)
        {
            dispute.ResolvedAt = DateTime.UtcNow;
        }

        dispute.Events.Add(new DisputeEvent
        {
            Status = request.Status,
            Message = string.IsNullOrWhiteSpace(dispute.AdminNotes)
                ? $"Dispute status updated to {request.Status}."
                : dispute.AdminNotes,
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

    public async Task<DisputeResponse> ReplyToDisputeAsync(
        int disputeId,
        string customerId,
        string customerName,
        ReplyRequest request)
    {
        var dispute = await _db.Disputes
            .Include(x => x.Customer)
            .Include(x => x.Transaction)
            .Include(x => x.Events)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == disputeId);

        if (dispute is null)
            throw new KeyNotFoundException("Dispute not found.");

        if (dispute.CustomerId != customerId)
            throw new UnauthorizedAccessException("You do not have access to this dispute.");

        if (dispute.Status != DisputeStatus.MoreInfoRequired)
            throw new BusinessRuleException("A reply can only be submitted when the dispute requires more information.");

        var sanitizedMessage = InputSanitizer.Sanitize(request.Message);
        if (string.IsNullOrEmpty(sanitizedMessage))
            throw new BusinessRuleException("Reply message cannot be blank.");

        dispute.Status = DisputeStatus.UnderReview;

        dispute.Events.Add(new DisputeEvent
        {
            Status = DisputeStatus.UnderReview,
            Message = sanitizedMessage,
            CreatedBy = customerName,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Customer replied to dispute {CaseNumber}, status back to UnderReview",
            dispute.CaseNumber);

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            if (admin.Email is not null)
            {
                await _notificationService.LogNotificationAsync(
                    admin.Email,
                    "Customer Reply Received",
                    $"Customer replied to dispute {dispute.CaseNumber} ({dispute.Transaction.MerchantName}). Case is now Under Review."
                );
            }
        }

        await _notificationService.PushDisputeEventAsync("role:Admin", "DisputeUpdated");

        return MapDispute(dispute);
    }

    public async Task<AttachmentResponse> AddAttachmentAsync(
        int disputeId,
        string userId,
        string uploaderName,
        IFormFile file)
    {
        if (file.Length > 5 * 1024 * 1024)
            throw new BusinessRuleException("File size cannot exceed 5 MB.");

        var allowed = new[] { "image/jpeg", "image/png", "image/gif", "application/pdf", "text/plain" };
        if (!allowed.Contains(file.ContentType))
            throw new BusinessRuleException("Only images, PDFs, and text files are allowed.");

        var dispute = await _db.Disputes
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == disputeId);

        if (dispute is null)
            throw new KeyNotFoundException("Dispute not found.");

        bool isAdmin = false;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
            isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        if (!isAdmin && dispute.CustomerId != userId)
            throw new UnauthorizedAccessException("You do not have access to this dispute.");

        if (!isAdmin && dispute.Status != DisputeStatus.MoreInfoRequired)
            throw new BusinessRuleException("Attachments can only be uploaded when more information has been requested.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var attachment = new DisputeAttachment
        {
            DisputeId = disputeId,
            FileName = InputSanitizer.Sanitize(Path.GetFileName(file.FileName)),
            ContentType = file.ContentType,
            FileData = ms.ToArray(),
            FileSize = file.Length,
            UploadedBy = uploaderName,
            UploadedAt = DateTime.UtcNow
        };

        _db.DisputeAttachments.Add(attachment);
        await _db.SaveChangesAsync();

        return new AttachmentResponse(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.FileSize,
            attachment.UploadedBy,
            attachment.UploadedAt
        );
    }

    public async Task<(byte[] Data, string ContentType, string FileName)> GetAttachmentAsync(
        int disputeId,
        string userId,
        bool isAdmin,
        int attachmentId)
    {
        var attachment = await _db.DisputeAttachments
            .Include(x => x.Dispute)
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.DisputeId == disputeId);

        if (attachment is null)
            throw new KeyNotFoundException("Attachment not found.");

        if (!isAdmin && attachment.Dispute.CustomerId != userId)
            throw new UnauthorizedAccessException("You do not have access to this attachment.");

        return (attachment.FileData, attachment.ContentType, attachment.FileName);
    }

    public async Task<AdminStatsResponse> GetAdminStatsAsync()
    {
        var all = await _db.Disputes.ToListAsync();

        var total = all.Count;

        var terminalStatuses = new[] { DisputeStatus.Rejected, DisputeStatus.Resolved };
        var openCount = all.Count(d => !terminalStatuses.Contains(d.Status));

        var todayUtc = DateTime.UtcNow.Date;
        var submittedToday = all.Count(d => d.CreatedAt.Date == todayUtc);

        var resolved = all
            .Where(d => d.ResolvedAt.HasValue)
            .Select(d => (d.ResolvedAt!.Value - d.CreatedAt).TotalDays)
            .ToList();

        var avgResolutionDays = resolved.Count > 0
            ? Math.Round(resolved.Average(), 1)
            : 0.0;

        var byStatus = all
            .GroupBy(d => d.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var status in Enum.GetNames<DisputeStatus>())
        {
            byStatus.TryAdd(status, 0);
        }

        return new AdminStatsResponse(total, openCount, submittedToday, avgResolutionDays, byStatus);
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
            dispute.Attachments
                .OrderBy(x => x.UploadedAt)
                .Select(a => new AttachmentResponse(
                    a.Id,
                    a.FileName,
                    a.ContentType,
                    a.FileSize,
                    a.UploadedBy,
                    a.UploadedAt
                ))
                .ToList(),
            customerName
        );
    }
}

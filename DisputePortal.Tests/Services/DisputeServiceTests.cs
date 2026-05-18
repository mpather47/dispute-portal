using DisputePortal.Api.Data;
using Xunit;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Exceptions;
using DisputePortal.Api.Models;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DisputePortal.Tests.Services;

public class DisputeServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mock.Setup(x => x.GetUsersInRoleAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<ApplicationUser>());
        return mock;
    }

    private static DisputeService CreateService(
        AppDbContext db,
        Mock<UserManager<ApplicationUser>>? umMock = null,
        Mock<INotificationService>? notifMock = null)
    {
        umMock ??= CreateUserManagerMock();
        notifMock ??= new Mock<INotificationService>();
        return new DisputeService(
            db,
            umMock.Object,
            notifMock.Object,
            NullLogger<DisputeService>.Instance);
    }

    private static async Task<(Account account, BankTransaction transaction)> SeedTransactionAsync(
        AppDbContext db,
        string userId = "user-1",
        bool isDisputable = true,
        bool hasDispute = false)
    {
        var account = new Account
        {
            AccountNumber = Guid.NewGuid().ToString(),
            AccountType = "Checking",
            UserId = userId
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var transaction = new BankTransaction
        {
            Reference = Guid.NewGuid().ToString(),
            MerchantName = "Test Merchant",
            Amount = 99.99m,
            Type = TransactionType.Debit,
            TransactionDate = DateTime.UtcNow,
            Description = "Test purchase",
            IsDisputable = isDisputable,
            AccountId = account.Id
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        if (hasDispute)
        {
            db.Disputes.Add(new Dispute
            {
                CaseNumber = $"DSP-SEED-{Guid.NewGuid().ToString("N")[..8]}",
                TransactionId = transaction.Id,
                CustomerId = userId,
                Reason = "Pre-existing",
                Status = DisputeStatus.Submitted
            });
            await db.SaveChangesAsync();
        }

        return (account, transaction);
    }

    private static async Task SetDisputeStatusDirectAsync(AppDbContext db, int disputeId, DisputeStatus status)
    {
        var entity = await db.Disputes.FindAsync(disputeId);
        entity!.Status = status;
        await db.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(AppDbContext db, string userId, string email = "user@test.com")
    {
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = email,
            NormalizedUserName = email.ToUpper(),
            Email = email,
            NormalizedEmail = email.ToUpper(),
            FullName = "Test User"
        });
        await db.SaveChangesAsync();
    }

    // ── CreateDisputeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateDisputeAsync_TransactionNotFound_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateDisputeAsync("user-1", new CreateDisputeRequest(999, "Fraud", "")));
    }

    [Fact]
    public async Task CreateDisputeAsync_TransactionBelongsToDifferentUser_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDb();
        var (_, tx) = await SeedTransactionAsync(db, userId: "owner");
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateDisputeAsync("other-user", new CreateDisputeRequest(tx.Id, "Fraud", "")));
    }

    [Fact]
    public async Task CreateDisputeAsync_TransactionNotDisputable_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        var (_, tx) = await SeedTransactionAsync(db, isDisputable: false);
        var service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", "")));
    }

    [Fact]
    public async Task CreateDisputeAsync_TransactionAlreadyDisputed_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        var (_, tx) = await SeedTransactionAsync(db, hasDispute: true);
        var service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", "")));
    }

    [Fact]
    public async Task CreateDisputeAsync_ValidRequest_ReturnsMappedResponse()
    {
        using var db = CreateDb();
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);

        var result = await service.CreateDisputeAsync(
            "user-1", new CreateDisputeRequest(tx.Id, "Fraud", "Details here"));

        Assert.Equal("Fraud", result.Reason);
        Assert.Equal("Details here", result.CustomerNotes);
        Assert.Equal(DisputeStatus.Submitted.ToString(), result.Status);
        Assert.Single(result.Events);
        Assert.StartsWith("DSP-", result.CaseNumber);
    }

    // ── GetDisputeByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDisputeByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetDisputeByIdAsync(999, "user-1", false));
    }

    [Fact]
    public async Task GetDisputeByIdAsync_WrongCustomer_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDb();
        var (_, tx) = await SeedTransactionAsync(db, userId: "owner");
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("owner")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("owner", new CreateDisputeRequest(tx.Id, "Fraud", ""));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetDisputeByIdAsync(created.Id, "intruder", false));
    }

    [Fact]
    public async Task GetDisputeByIdAsync_AdminBypassesOwnerCheck()
    {
        using var db = CreateDb();
        var (_, tx) = await SeedTransactionAsync(db, userId: "owner");
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("owner")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("owner", new CreateDisputeRequest(tx.Id, "Fraud", ""));

        var result = await service.GetDisputeByIdAsync(created.Id, "admin-user", true);

        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetDisputeByIdAsync_Owner_ReturnsDispute()
    {
        using var db = CreateDb();
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Reason", ""));

        var result = await service.GetDisputeByIdAsync(created.Id, "user-1", false);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Reason", result.Reason);
    }

    // ── UpdateDisputeStatusAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateDisputeStatusAsync_DisputeNotFound_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateDisputeStatusAsync(
                999, new UpdateDisputeStatusRequest(DisputeStatus.UnderReview, null), "Admin"));
    }

    [Fact]
    public async Task UpdateDisputeStatusAsync_InvalidTransition_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));

        // Submitted → Resolved is not a valid transition
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateDisputeStatusAsync(
                created.Id,
                new UpdateDisputeStatusRequest(DisputeStatus.Resolved, null),
                "Admin"));
    }

    [Fact]
    public async Task UpdateDisputeStatusAsync_ValidTransition_UpdatesStatusAndAddsEvent()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));

        var result = await service.UpdateDisputeStatusAsync(
            created.Id,
            new UpdateDisputeStatusRequest(DisputeStatus.UnderReview, "Reviewing now"),
            "ReviewerAdmin");

        Assert.Equal(DisputeStatus.UnderReview.ToString(), result.Status);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("ReviewerAdmin", result.Events.Last().CreatedBy);
        Assert.Equal("Reviewing now", result.Events.Last().Message);
    }

    [Fact]
    public async Task UpdateDisputeStatusAsync_ApprovalSetsResolvedAt()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));

        // Move to UnderReview first, then Approved
        await service.UpdateDisputeStatusAsync(
            created.Id, new UpdateDisputeStatusRequest(DisputeStatus.UnderReview, null), "Admin");
        var result = await service.UpdateDisputeStatusAsync(
            created.Id, new UpdateDisputeStatusRequest(DisputeStatus.Approved, null), "Admin");

        Assert.NotNull(result.ResolvedAt);
    }

    // ── GetAllDisputesForAdminAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAllDisputesForAdminAsync_ReturnsPaginatedResult()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);

        for (int i = 0; i < 5; i++)
        {
            var (_, tx) = await SeedTransactionAsync(db, "user-1");
            await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, $"Reason {i}", ""));
        }

        var page1 = await service.GetAllDisputesForAdminAsync(1, 3);
        var page2 = await service.GetAllDisputesForAdminAsync(2, 3);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page2.Page);
    }

    [Fact]
    public async Task GetAllDisputesForAdminAsync_EmptyDatabase_ReturnsEmptyPage()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetAllDisputesForAdminAsync(1, 20);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    // ── GetCustomerDisputesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerDisputesAsync_ReturnsOnlyOwnedDisputes()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        await SeedUserAsync(db, "user-2", "user2@test.com");
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);

        var (_, tx1) = await SeedTransactionAsync(db, "user-1");
        var (_, tx2) = await SeedTransactionAsync(db, "user-2");
        await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx1.Id, "Fraud", ""));
        await service.CreateDisputeAsync("user-2", new CreateDisputeRequest(tx2.Id, "Fraud", ""));

        var disputes = await service.GetCustomerDisputesAsync("user-1");

        Assert.Single(disputes);
        Assert.All(disputes, d => Assert.Equal(99.99m, d.Amount));
    }

    [Fact]
    public async Task GetCustomerDisputesAsync_NoDisputes_ReturnsEmptyList()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetCustomerDisputesAsync("user-with-no-disputes");

        Assert.Empty(result);
    }

    // ── GetCustomerTransactionsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetCustomerTransactionsAsync_ReturnsOnlyOwnedTransactions()
    {
        using var db = CreateDb();
        await SeedTransactionAsync(db, "user-1");
        await SeedTransactionAsync(db, "user-1");
        await SeedTransactionAsync(db, "user-2");
        var service = CreateService(db);

        var result = await service.GetCustomerTransactionsAsync("user-1");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetCustomerTransactionsAsync_MarksDisputedTransactions()
    {
        using var db = CreateDb();
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var (_, tx) = await SeedTransactionAsync(db);
        await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));

        var result = await service.GetCustomerTransactionsAsync("user-1");

        Assert.True(result.Single().HasDispute);
    }

    // ── ReplyToDisputeAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ReplyToDisputeAsync_NotFound_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ReplyToDisputeAsync(999, "user-1", "User", new ReplyRequest("hello")));
    }

    [Fact]
    public async Task ReplyToDisputeAsync_WrongUser_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "owner", "owner@test.com");
        var (_, tx) = await SeedTransactionAsync(db, userId: "owner");
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("owner")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("owner", new CreateDisputeRequest(tx.Id, "Fraud", ""));
        await SetDisputeStatusDirectAsync(db, created.Id, DisputeStatus.MoreInfoRequired);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ReplyToDisputeAsync(created.Id, "intruder", "Intruder", new ReplyRequest("hello")));
    }

    [Fact]
    public async Task ReplyToDisputeAsync_WrongStatus_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));
        // Status is Submitted — not MoreInfoRequired

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ReplyToDisputeAsync(created.Id, "user-1", "User", new ReplyRequest("hello")));
    }

    [Fact]
    public async Task ReplyToDisputeAsync_BlankMessage_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));
        await SetDisputeStatusDirectAsync(db, created.Id, DisputeStatus.MoreInfoRequired);

        // HTML-only input sanitizes to empty string
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ReplyToDisputeAsync(created.Id, "user-1", "User", new ReplyRequest("<b></b>")));
    }

    [Fact]
    public async Task ReplyToDisputeAsync_ValidRequest_TransitionsToUnderReview()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var (_, tx) = await SeedTransactionAsync(db);
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));
        await SetDisputeStatusDirectAsync(db, created.Id, DisputeStatus.MoreInfoRequired);

        var result = await service.ReplyToDisputeAsync(
            created.Id, "user-1", "Test User", new ReplyRequest("Here are the details."));

        Assert.Equal(DisputeStatus.UnderReview.ToString(), result.Status);
        Assert.Equal("Here are the details.", result.Events.Last().Message);
        Assert.Equal("Test User", result.Events.Last().CreatedBy);
    }

    // ── GetAdminStatsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminStatsAsync_EmptyDatabase_ReturnsZeroes()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var stats = await service.GetAdminStatsAsync();

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.OpenCount);
        Assert.Equal(0, stats.SubmittedToday);
        Assert.Equal(0.0, stats.AvgResolutionDays);
    }

    [Fact]
    public async Task GetAdminStatsAsync_WithDisputes_ReturnsCorrectCounts()
    {
        using var db = CreateDb();
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);

        var (_, tx1) = await SeedTransactionAsync(db, "user-1");
        var (_, tx2) = await SeedTransactionAsync(db, "user-1");
        var (_, tx3) = await SeedTransactionAsync(db, "user-1");

        var d1 = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx1.Id, "R1", ""));
        var d2 = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx2.Id, "R2", ""));
        await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx3.Id, "R3", ""));

        // Resolve d1 and set ResolvedAt so avg calculation has data
        await SetDisputeStatusDirectAsync(db, d1.Id, DisputeStatus.Resolved);
        var d1Entity = await db.Disputes.FindAsync(d1.Id);
        d1Entity!.ResolvedAt = d1Entity.CreatedAt.AddDays(4);
        await db.SaveChangesAsync();

        // Reject d2
        await SetDisputeStatusDirectAsync(db, d2.Id, DisputeStatus.Rejected);

        var stats = await service.GetAdminStatsAsync();

        Assert.Equal(3, stats.Total);
        Assert.Equal(1, stats.OpenCount); // only d3 (Submitted) is open; Rejected+Resolved are terminal
        Assert.Equal(3, stats.SubmittedToday);
        Assert.Equal(4.0, stats.AvgResolutionDays);
        Assert.Equal(1, stats.ByStatus["Resolved"]);
        Assert.Equal(1, stats.ByStatus["Rejected"]);
        Assert.Equal(1, stats.ByStatus["Submitted"]);
    }

    // ── AddAttachmentAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddAttachmentAsync_NonMoreInfoRequired_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        await SeedUserAsync(db, "user-1");
        var umMock = CreateUserManagerMock();
        umMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(db, umMock);
        var (_, tx) = await SeedTransactionAsync(db);
        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequest(tx.Id, "Fraud", ""));
        // Dispute is in Submitted status — not MoreInfoRequired

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddAttachmentAsync(created.Id, "user-1", "Test User", fileMock.Object));
    }
}

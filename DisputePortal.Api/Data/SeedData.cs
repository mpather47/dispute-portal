using DisputePortal.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace DisputePortal.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Customer"))
            await roleManager.CreateAsync(new IdentityRole("Customer"));

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var customer = await EnsureCustomer(userManager, "customer@test.com", "Customer User");
        var customer2 = await EnsureCustomer(userManager, "alice.chen@test.com", "Alice Chen");
        var customer3 = await EnsureCustomer(userManager, "marcus.williams@test.com", "Marcus Williams");
        var customer4 = await EnsureCustomer(userManager, "priya.sharma@test.com", "Priya Sharma");
        var customer5 = await EnsureCustomer(userManager, "james.okafor@test.com", "James Okafor");

        var admin = await userManager.FindByEmailAsync("admin@test.com");
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FullName = "Bank Admin",
                Email = "admin@test.com",
                UserName = "admin@test.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Password123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (db.Accounts.Any())
            return;

        // ── Account 1: Customer User ────────────────────────────────────────
        var account1 = new Account { AccountNumber = "1234567890", AccountType = "Cheque", Balance = 15250.75m, UserId = customer.Id };
        var t1_1 = Txn("TXN-10001", "Takealot", -899.99m, -2);
        var t1_2 = Txn("TXN-10002", "Uber", -124.50m, -4);
        var t1_3 = Txn("TXN-10003", "Salary Payment", 25000.00m, -7, TransactionType.Credit, false);
        var t1_4 = Txn("TXN-10004", "Netflix", -199.00m, -10);
        account1.Transactions.AddRange([t1_1, t1_2, t1_3, t1_4]);

        // ── Account 2: Alice Chen ────────────────────────────────────────────
        var account2 = new Account { AccountNumber = "2345678901", AccountType = "Savings", Balance = 8400.00m, UserId = customer2.Id };
        var t2_1 = Txn("TXN-20001", "Amazon", -1250.00m, -1);
        var t2_2 = Txn("TXN-20002", "Woolworths Food", -340.00m, -3);
        var t2_3 = Txn("TXN-20003", "Steam", -599.99m, -5);
        var t2_4 = Txn("TXN-20004", "Checkers", -187.50m, -8);
        var t2_5 = Txn("TXN-20005", "FNB ATM Withdrawal", -500.00m, -11);
        account2.Transactions.AddRange([t2_1, t2_2, t2_3, t2_4, t2_5]);

        // ── Account 3: Marcus Williams ───────────────────────────────────────
        var account3 = new Account { AccountNumber = "3456789012", AccountType = "Cheque", Balance = 32100.00m, UserId = customer3.Id };
        var t3_1 = Txn("TXN-30001", "Apple Store", -2499.00m, -1);
        var t3_2 = Txn("TXN-30002", "Makro", -4300.00m, -3);
        var t3_3 = Txn("TXN-30003", "Bolt", -89.00m, -6);
        var t3_4 = Txn("TXN-30004", "Spotify", -79.99m, -9);
        var t3_5 = Txn("TXN-30005", "Engen Garage", -420.00m, -12);
        account3.Transactions.AddRange([t3_1, t3_2, t3_3, t3_4, t3_5]);

        // ── Account 4: Priya Sharma ──────────────────────────────────────────
        var account4 = new Account { AccountNumber = "4567890123", AccountType = "Savings", Balance = 54000.00m, UserId = customer4.Id };
        var t4_1 = Txn("TXN-40001", "Edgars", -1890.00m, -2);
        var t4_2 = Txn("TXN-40002", "Dischem", -312.00m, -5);
        var t4_3 = Txn("TXN-40003", "Pick n Pay", -675.40m, -8);
        var t4_4 = Txn("TXN-40004", "KFC", -180.00m, -10);
        account4.Transactions.AddRange([t4_1, t4_2, t4_3, t4_4]);

        // ── Account 5: James Okafor ──────────────────────────────────────────
        var account5 = new Account { AccountNumber = "5678901234", AccountType = "Cheque", Balance = 9200.00m, UserId = customer5.Id };
        var t5_1 = Txn("TXN-50001", "Superbalist", -750.00m, -1);
        var t5_2 = Txn("TXN-50002", "Incredible Connection", -5999.00m, -4);
        var t5_3 = Txn("TXN-50003", "Capitec ATM", -1000.00m, -7);
        var t5_4 = Txn("TXN-50004", "Mr Price", -429.99m, -9);
        account5.Transactions.AddRange([t5_1, t5_2, t5_3, t5_4]);

        db.Accounts.AddRange(account1, account2, account3, account4, account5);
        await db.SaveChangesAsync();

        // ── Disputes ─────────────────────────────────────────────────────────
        var disputes = new List<Dispute>
        {
            // Customer User
            Dispute("DSP-SEED-001", t1_1, customer.Id, "Fraud", "Did not make this purchase.", DisputeStatus.Submitted, -1),
            Dispute("DSP-SEED-002", t1_2, customer.Id, "Duplicate Charge", "Charged twice for same ride.", DisputeStatus.UnderReview, -3,
                adminNote: "Checking with Uber on their end.", reviewedAt: -2),

            // Alice Chen
            Dispute("DSP-SEED-003", t2_1, customer2.Id, "Item Not Received", "Order never arrived.", DisputeStatus.Submitted, -1),
            Dispute("DSP-SEED-004", t2_2, customer2.Id, "Wrong Amount", "Charged R340 but receipt shows R240.", DisputeStatus.UnderReview, -4,
                reviewedAt: -3),
            Dispute("DSP-SEED-005", t2_3, customer2.Id, "Unauthorized", "Account was compromised.", DisputeStatus.MoreInfoRequired, -6,
                adminNote: "Please provide proof of compromise.", reviewedAt: -5),
            Dispute("DSP-SEED-006", t2_4, customer2.Id, "Fraud", "Card skimmed at ATM.", DisputeStatus.Approved, -10,
                adminNote: "Refund approved. Fraud confirmed.", reviewedAt: -9, resolvedAt: -8),
            Dispute("DSP-SEED-007", t2_5, customer2.Id, "Dispute ATM Amount", "Wrong amount dispensed.", DisputeStatus.Rejected, -14,
                adminNote: "ATM balanced correctly. No discrepancy found.", reviewedAt: -13, resolvedAt: -12),

            // Marcus Williams
            Dispute("DSP-SEED-008", t3_1, customer3.Id, "Counterfeit Item", "Item arrived damaged and not as described.", DisputeStatus.Submitted, -1),
            Dispute("DSP-SEED-009", t3_2, customer3.Id, "Fraud", "Did not authorise this transaction.", DisputeStatus.UnderReview, -5,
                reviewedAt: -4),
            Dispute("DSP-SEED-010", t3_3, customer3.Id, "Overcharge", "Correct fare is R45, not R89.", DisputeStatus.MoreInfoRequired, -7,
                adminNote: "Please share trip receipt from the app.", reviewedAt: -6),
            Dispute("DSP-SEED-011", t3_4, customer3.Id, "Unauthorized", "Never subscribed to this service.", DisputeStatus.Approved, -12,
                adminNote: "Refund approved.", reviewedAt: -11, resolvedAt: -10),
            Dispute("DSP-SEED-012", t3_5, customer3.Id, "Wrong Amount", "Pump showed R390, charged R420.", DisputeStatus.Resolved, -20,
                adminNote: "Resolved in customer's favour. Difference credited.", reviewedAt: -19, resolvedAt: -18),

            // Priya Sharma
            Dispute("DSP-SEED-013", t4_1, customer4.Id, "Item Not Received", "Order cancelled but amount not refunded.", DisputeStatus.Submitted, -2),
            Dispute("DSP-SEED-014", t4_2, customer4.Id, "Duplicate Charge", "Two debits on same day for same purchase.", DisputeStatus.UnderReview, -6,
                reviewedAt: -5),
            Dispute("DSP-SEED-015", t4_3, customer4.Id, "Fraud", "Unknown transaction.", DisputeStatus.MoreInfoRequired, -9,
                adminNote: "Confirm your location on that date.", reviewedAt: -8),
            Dispute("DSP-SEED-016", t4_4, customer4.Id, "Wrong Amount", "Should be R120 not R180.", DisputeStatus.Rejected, -15,
                adminNote: "Transaction matches POS receipt. No error found.", reviewedAt: -14, resolvedAt: -13),

            // James Okafor
            Dispute("DSP-SEED-017", t5_1, customer5.Id, "Unauthorized", "Card reported stolen but charge went through.", DisputeStatus.Submitted, -1),
            Dispute("DSP-SEED-018", t5_2, customer5.Id, "Fraud", "Never visited this store.", DisputeStatus.UnderReview, -5,
                reviewedAt: -4),
            Dispute("DSP-SEED-019", t5_3, customer5.Id, "Wrong Amount", "Withdrew R800, charged R1000.", DisputeStatus.Approved, -9,
                adminNote: "ATM discrepancy confirmed. Refund approved.", reviewedAt: -8, resolvedAt: -7),
            Dispute("DSP-SEED-020", t5_4, customer5.Id, "Item Not Received", "Paid but did not receive goods.", DisputeStatus.Resolved, -16,
                adminNote: "Merchant confirmed refund issued.", reviewedAt: -15, resolvedAt: -14),
        };

        db.Disputes.AddRange(disputes);
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureCustomer(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Password123!");
            await userManager.AddToRoleAsync(user, "Customer");
        }
        return user;
    }

    private static BankTransaction Txn(
        string reference,
        string merchant,
        decimal amount,
        int daysAgo,
        TransactionType type = TransactionType.Debit,
        bool isDisputable = true) =>
        new()
        {
            Reference = reference,
            MerchantName = merchant,
            Amount = amount,
            Type = type,
            TransactionDate = DateTime.UtcNow.AddDays(daysAgo),
            Description = type == TransactionType.Credit ? "Credit" : "Purchase",
            IsDisputable = isDisputable
        };

    private static Dispute Dispute(
        string caseNumber,
        BankTransaction transaction,
        string customerId,
        string reason,
        string customerNotes,
        DisputeStatus status,
        int createdDaysAgo,
        string? adminNote = null,
        int? reviewedAt = null,
        int? resolvedAt = null)
    {
        var d = new Dispute
        {
            CaseNumber = caseNumber,
            Transaction = transaction,
            CustomerId = customerId,
            Reason = reason,
            CustomerNotes = customerNotes,
            AdminNotes = adminNote,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(createdDaysAgo),
            ResolvedAt = resolvedAt.HasValue ? DateTime.UtcNow.AddDays(resolvedAt.Value) : null
        };

        d.Events.Add(new DisputeEvent
        {
            Status = DisputeStatus.Submitted,
            Message = "Dispute submitted by customer.",
            CreatedBy = "Customer",
            CreatedAt = DateTime.UtcNow.AddDays(createdDaysAgo)
        });

        if (reviewedAt.HasValue)
        {
            d.Events.Add(new DisputeEvent
            {
                Status = DisputeStatus.UnderReview,
                Message = "Case taken under review.",
                CreatedBy = "admin@test.com",
                CreatedAt = DateTime.UtcNow.AddDays(reviewedAt.Value)
            });
        }

        if (status == DisputeStatus.MoreInfoRequired && adminNote is not null)
        {
            d.Events.Add(new DisputeEvent
            {
                Status = DisputeStatus.MoreInfoRequired,
                Message = adminNote,
                CreatedBy = "admin@test.com",
                CreatedAt = DateTime.UtcNow.AddDays(reviewedAt!.Value - 1)
            });
        }

        if (resolvedAt.HasValue && status is DisputeStatus.Approved or DisputeStatus.Rejected or DisputeStatus.Resolved)
        {
            d.Events.Add(new DisputeEvent
            {
                Status = status,
                Message = adminNote ?? $"Dispute {status}.",
                CreatedBy = "admin@test.com",
                CreatedAt = DateTime.UtcNow.AddDays(resolvedAt.Value)
            });
        }

        return d;
    }
}

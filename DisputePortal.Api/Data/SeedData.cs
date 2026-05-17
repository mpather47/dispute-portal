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
        {
            await roleManager.CreateAsync(new IdentityRole("Customer"));
        }

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var customer = await userManager.FindByEmailAsync("customer@test.com");

        if (customer is null)
        {
            customer = new ApplicationUser
            {
                FullName = "Customer User",
                Email = "customer@test.com",
                UserName = "customer@test.com",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(customer, "Password123!");
            await userManager.AddToRoleAsync(customer, "Customer");
        }

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
        {
            return;
        }

        var account = new Account
        {
            AccountNumber = "1234567890",
            AccountType = "Cheque",
            Balance = 15250.75m,
            UserId = customer.Id
        };

        account.Transactions.AddRange(new List<BankTransaction>
        {
            new()
            {
                Reference = "TXN-10001",
                MerchantName = "Takealot",
                Amount = -899.99m,
                Type = TransactionType.Debit,
                TransactionDate = DateTime.UtcNow.AddDays(-2),
                Description = "Online purchase",
                IsDisputable = true
            },
            new()
            {
                Reference = "TXN-10002",
                MerchantName = "Uber",
                Amount = -124.50m,
                Type = TransactionType.Debit,
                TransactionDate = DateTime.UtcNow.AddDays(-4),
                Description = "Transport",
                IsDisputable = true
            },
            new()
            {
                Reference = "TXN-10003",
                MerchantName = "Salary Payment",
                Amount = 25000.00m,
                Type = TransactionType.Credit,
                TransactionDate = DateTime.UtcNow.AddDays(-7),
                Description = "Monthly salary",
                IsDisputable = false
            },
            new()
            {
                Reference = "TXN-10004",
                MerchantName = "Netflix",
                Amount = -199.00m,
                Type = TransactionType.Debit,
                TransactionDate = DateTime.UtcNow.AddDays(-10),
                Description = "Subscription",
                IsDisputable = true
            }
        });

        db.Accounts.Add(account);
        await db.SaveChangesAsync();
    }
}
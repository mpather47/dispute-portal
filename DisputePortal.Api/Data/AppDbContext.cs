using DisputePortal.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DisputePortal.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<BankTransaction> Transactions => Set<BankTransaction>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<DisputeEvent> DisputeEvents => Set<DisputeEvent>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<DisputeAttachment> DisputeAttachments => Set<DisputeAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>()
            .HasIndex(x => x.AccountNumber)
            .IsUnique();

        modelBuilder.Entity<BankTransaction>()
            .HasIndex(x => x.Reference)
            .IsUnique();

        modelBuilder.Entity<BankTransaction>()
            .HasOne(x => x.Dispute)
            .WithOne(x => x.Transaction)
            .HasForeignKey<Dispute>(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Dispute>()
            .HasIndex(x => x.CaseNumber)
            .IsUnique();

        modelBuilder.Entity<Dispute>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Account>()
            .Property(x => x.Balance)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<BankTransaction>()
            .Property(x => x.Amount)
            .HasColumnType("decimal(18,2)");
    }
}
using Elsa.Persistence.EFCore;
using Elsa.Workflows.Admission;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Slack.SocketMode.Persistence;

/// <summary>Separate PostgreSQL discard receipts; requires already-provisioned Admission subscriptions.</summary>
public sealed class SlackSocketReceiptElsaDbContext(DbContextOptions<SlackSocketReceiptElsaDbContext> options, IServiceProvider serviceProvider)
    : ElsaDbContextBase(options, serviceProvider)
{
    public const string HistoryTable = "__SlackSocketReceiptsMigrationsHistory";
    internal DbSet<SlackSocketDiscardReceipt> Receipts => Set<SlackSocketDiscardReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var subscription = modelBuilder.Entity<SlackSocketReceiptSubscriptionKey>();
        subscription.ToTable("AdmissionSubscriptions", table => table.ExcludeFromMigrations());
        subscription.HasKey(x => x.Id);
        subscription.Property(x => x.Id).HasMaxLength(256);

        var receipt = modelBuilder.Entity<SlackSocketDiscardReceipt>();
        receipt.ToTable("SlackSocketDiscardReceipts");
        receipt.HasKey(x => x.Id);
        receipt.Property(x => x.Id).HasMaxLength(256);
        receipt.Property(x => x.SubscriptionId).HasMaxLength(256);
        receipt.Property(x => x.TenantId).HasMaxLength(256);
        receipt.Property(x => x.EnvironmentId).HasMaxLength(256);
        receipt.Property(x => x.ProviderEventId).HasMaxLength(AdmissionLimits.ProviderEventIdBytes);
        receipt.Property(x => x.IdentityHash).HasMaxLength(64);
        receipt.Property(x => x.BindingFingerprint).HasMaxLength(64);
        receipt.Property(x => x.ConfigurationFingerprint).HasMaxLength(64);
        receipt.Property(x => x.PayloadFingerprint).HasMaxLength(64);
        receipt.Property(x => x.EventFingerprint).HasMaxLength(64);
        receipt.Property(x => x.Reason).HasConversion<string>().HasMaxLength(32);
        receipt.Property(x => x.Revision).IsConcurrencyToken();
        receipt.HasIndex(x => x.IdentityHash).IsUnique();
        receipt.HasIndex(x => new { x.TenantId, x.EnvironmentId, x.Id });
        receipt.HasOne<SlackSocketReceiptSubscriptionKey>().WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        base.OnModelCreating(modelBuilder);
    }
}

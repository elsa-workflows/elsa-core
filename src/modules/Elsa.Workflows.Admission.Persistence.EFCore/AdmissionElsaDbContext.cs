using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Admission.Persistence.EFCore;

/// <summary>A distinct ledger context; no credential values or invocable authority are persisted here.</summary>
public sealed class AdmissionElsaDbContext(DbContextOptions<AdmissionElsaDbContext> options, IServiceProvider serviceProvider)
    : ElsaDbContextBase(options, serviceProvider)
{
    public DbSet<AdmissionSubscription> Subscriptions { get; set; } = null!;
    public DbSet<AdmissionRecord> Admissions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureAdmissionModel(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Shared by the context and the provider migration snapshot.</summary>
    public static void ConfigureAdmissionModel(ModelBuilder modelBuilder)
    {
        var subscription = modelBuilder.Entity<AdmissionSubscription>();
        subscription.ToTable("AdmissionSubscriptions", table =>
        {
            table.HasCheckConstraint("CK_AdmissionSubscriptions_Capacity", "\"ActiveReservations\" >= 0 AND \"RetainedRecords\" >= \"ActiveReservations\"");
        });
        subscription.HasKey(x => x.Id);
        subscription.Property(x => x.Id).HasMaxLength(256);
        subscription.Ignore(x => x.Configuration);
        subscription.Property(x => x.ConfigurationFingerprint).HasMaxLength(64);
        subscription.Property(x => x.ReconciliationCode).HasMaxLength(256);
        subscription.Property(x => x.Revision).IsConcurrencyToken();
        subscription.Property<string>("TenantId").HasMaxLength(256).IsRequired();
        subscription.Property<string>("EnvironmentId").HasMaxLength(256).IsRequired();
        subscription.HasIndex("TenantId", "EnvironmentId", nameof(AdmissionSubscription.Id));

        var record = modelBuilder.Entity<AdmissionRecord>();
        record.ToTable("Admissions");
        record.HasKey(x => x.Id);
        record.Property(x => x.Id).HasMaxLength(256);
        record.Property(x => x.SubscriptionId).HasMaxLength(256);
        record.Property(x => x.IdentityHash).HasMaxLength(64);
        record.Property(x => x.ProviderEventId).HasMaxLength(256);
        record.Property(x => x.ConfigurationFingerprint).HasMaxLength(64);
        record.Property(x => x.WorkflowInstanceId).HasMaxLength(256);
        record.Property(x => x.AttemptId).HasMaxLength(256);
        record.Property(x => x.CheckpointFingerprint).HasMaxLength(64);
        record.Property(x => x.RecoveryCode).HasMaxLength(256);
        record.Property(x => x.AuditReference).HasMaxLength(256);
        record.Property(x => x.PayloadFingerprint).HasMaxLength(64);
        record.Property(x => x.EventFingerprint).HasMaxLength(64);
        record.Property(x => x.Revision).IsConcurrencyToken();
        record.Property(x => x.State).HasConversion<string>().HasMaxLength(32);
        record.Property(x => x.TerminalDisposition).HasConversion<string>().HasMaxLength(32);
        record.Property<string>("TenantId").HasMaxLength(256).IsRequired();
        record.Property<string>("EnvironmentId").HasMaxLength(256).IsRequired();
        record.HasIndex(x => x.IdentityHash).IsUnique();
        record.HasIndex(x => x.WorkflowInstanceId).IsUnique();
        record.HasIndex("TenantId", "EnvironmentId", nameof(AdmissionRecord.State), nameof(AdmissionRecord.AdmittedAt), nameof(AdmissionRecord.Id));
        record.HasIndex(x => new { x.SubscriptionId, x.State, x.TerminalAt, x.Id });
        record.HasOne<AdmissionSubscription>().WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
    }
}

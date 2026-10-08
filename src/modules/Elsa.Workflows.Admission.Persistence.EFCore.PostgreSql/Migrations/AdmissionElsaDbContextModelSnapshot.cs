using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Migrations;

[DbContext(typeof(AdmissionElsaDbContext))]
public sealed class AdmissionElsaDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => InitialAdmissionModel.Build(modelBuilder);
}

// Frozen migration model. Do not call the mutable production model configuration here.
internal static class InitialAdmissionModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ElsaDbContextBase.ElsaSchema);
        modelBuilder.HasAnnotation("ProductVersion", "10.0.9").HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
        modelBuilder.Entity("Elsa.Workflows.Admission.AdmissionSubscription", entity =>
        {
            entity.Property<string>("Id").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("ConfigurationJson").IsRequired().HasColumnType("text");
            entity.Property<string>("ConfigurationFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<long>("Revision").IsConcurrencyToken().HasColumnType("bigint");
            entity.Property<long>("ActivationEpoch").HasColumnType("bigint");
            entity.Property<bool>("Active").HasColumnType("boolean");
            entity.Property<bool>("Retired").HasColumnType("boolean");
            entity.Property<bool>("BootstrapVerified").HasColumnType("boolean");
            entity.Property<int>("ActiveReservations").HasColumnType("integer");
            entity.Property<int>("RetainedRecords").HasColumnType("integer");
            entity.Property<string>("ReconciliationCode").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("EnvironmentId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.HasKey("Id");
            entity.HasIndex("TenantId", "EnvironmentId", "Id");
            entity.ToTable("AdmissionSubscriptions", ElsaDbContextBase.ElsaSchema, table => table.HasCheckConstraint("CK_AdmissionSubscriptions_Capacity", "\"ActiveReservations\" >= 0 AND \"RetainedRecords\" >= \"ActiveReservations\""));
        });
        modelBuilder.Entity("Elsa.Workflows.Admission.AdmissionRecord", entity =>
        {
            entity.Property<string>("Id").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("SubscriptionId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("IdentityHash").HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("ProviderEventId").HasMaxLength(1024).HasColumnType("character varying(1024)");
            entity.Property<string>("Payload").HasColumnType("text");
            entity.Property<string>("PayloadFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("EventFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("AdmittedConfigurationJson").IsRequired().HasColumnType("text");
            entity.Property<string>("ConfigurationFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<long>("ActivationEpoch").HasColumnType("bigint");
            entity.Property<DateTimeOffset>("AdmittedAt").HasColumnType("timestamp with time zone");
            entity.Property<DateTimeOffset>("EventOccurredAt").HasColumnType("timestamp with time zone");
            entity.Property<long>("Revision").IsConcurrencyToken().HasColumnType("bigint");
            entity.Property<string>("State").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            entity.Property<string>("WorkflowInstanceId").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("AttemptId").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<bool>("AuthorityOutstanding").HasColumnType("boolean");
            entity.Property<string>("CheckpointFingerprint").HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("BookmarkIdsJson").HasColumnType("text");
            entity.Property<string>("RecoveryCode").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("AuditReference").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("TerminalDisposition").HasMaxLength(32).HasColumnType("character varying(32)");
            entity.Property<DateTimeOffset?>("TerminalAt").HasColumnType("timestamp with time zone");
            entity.Property<bool>("ActiveReservationReleased").HasColumnType("boolean");
            entity.Property<bool>("RetainedRecordReleased").HasColumnType("boolean");
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("EnvironmentId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.HasKey("Id");
            entity.HasIndex("IdentityHash").IsUnique();
            entity.HasIndex("WorkflowInstanceId").IsUnique();
            entity.HasIndex("TenantId", "EnvironmentId", "Id").HasFilter("\"State\" <> 'Terminal'");
            entity.HasIndex("SubscriptionId", "State", "TerminalAt", "Id");
            entity.ToTable("Admissions", ElsaDbContextBase.ElsaSchema);
        });
        modelBuilder.Entity("Elsa.Workflows.Admission.AdmissionRecord", entity =>
        {
            entity.HasOne("Elsa.Workflows.Admission.AdmissionSubscription", null).WithMany().HasForeignKey("SubscriptionId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}

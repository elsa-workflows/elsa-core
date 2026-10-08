using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Slack.SocketMode.Persistence.Migrations;

[DbContext(typeof(SlackSocketReceiptElsaDbContext))]
public sealed class SlackSocketReceiptElsaDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => InitialSlackSocketReceiptModel.Build(modelBuilder);
}

// Frozen model. Never delegate migration history to mutable runtime configuration.
internal static class InitialSlackSocketReceiptModel
{
    internal static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ElsaDbContextBase.ElsaSchema);
        modelBuilder.HasAnnotation("ProductVersion", "10.0.9").HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
        modelBuilder.Entity("Elsa.Slack.SocketMode.Persistence.SlackSocketReceiptSubscriptionKey", entity =>
        {
            entity.Property<string>("Id").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.HasKey("Id");
            entity.ToTable("AdmissionSubscriptions", ElsaDbContextBase.ElsaSchema, table => table.ExcludeFromMigrations());
        });
        modelBuilder.Entity("Elsa.Slack.SocketMode.Persistence.SlackSocketDiscardReceipt", entity =>
        {
            entity.Property<string>("Id").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("SubscriptionId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("EnvironmentId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("IdentityHash").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("ProviderEventId").IsRequired().HasMaxLength(1024).HasColumnType("character varying(1024)");
            entity.Property<string>("Reason").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            entity.Property<string>("BindingFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("ConfigurationFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("ConfigurationJson").IsRequired().HasColumnType("text");
            entity.Property<long>("ActivationEpoch").HasColumnType("bigint");
            entity.Property<string>("PayloadFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<string>("EventFingerprint").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<DateTimeOffset>("EventOccurredAt").HasColumnType("timestamp with time zone");
            entity.Property<DateTimeOffset>("DecisionAt").HasColumnType("timestamp with time zone");
            entity.Property<long>("Revision").IsConcurrencyToken().HasColumnType("bigint");
            entity.HasKey("Id");
            entity.HasIndex("IdentityHash").IsUnique();
            entity.HasIndex("SubscriptionId");
            entity.HasIndex("TenantId", "EnvironmentId", "Id");
            entity.ToTable("SlackSocketDiscardReceipts", ElsaDbContextBase.ElsaSchema);
            entity.HasOne("Elsa.Slack.SocketMode.Persistence.SlackSocketReceiptSubscriptionKey", null).WithMany()
                .HasForeignKey("SubscriptionId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}

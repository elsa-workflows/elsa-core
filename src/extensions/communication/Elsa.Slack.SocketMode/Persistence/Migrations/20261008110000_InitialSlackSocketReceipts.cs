using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Slack.SocketMode.Persistence.Migrations;

[DbContext(typeof(SlackSocketReceiptElsaDbContext))]
[Migration("20261008110000_InitialSlackSocketReceipts")]
public sealed class InitialSlackSocketReceipts(IElsaDbContextSchema schema) : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(schema.Schema);
        // The restrict FK intentionally fails if Admission has not been provisioned.
        // This migration must never create, rewrite or backfill Admission tables.
        migrationBuilder.CreateTable(name: "SlackSocketDiscardReceipts", schema: schema.Schema, columns: table => new
        {
            Id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            SubscriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            TenantId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            EnvironmentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            IdentityHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            ProviderEventId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
            Reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            BindingFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            ConfigurationFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            ConfigurationJson = table.Column<string>(type: "text", nullable: false),
            ActivationEpoch = table.Column<long>(type: "bigint", nullable: false),
            PayloadFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            EventFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            EventOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            DecisionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            Revision = table.Column<long>(type: "bigint", nullable: false),
        }, constraints: table =>
        {
            table.PrimaryKey("PK_SlackSocketDiscardReceipts", x => x.Id);
            table.ForeignKey("FK_SlackSocketDiscardReceipts_AdmissionSubscriptions_SubscriptionId", x => x.SubscriptionId,
                principalTable: "AdmissionSubscriptions", principalColumn: "Id", principalSchema: schema.Schema, onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_SlackSocketDiscardReceipts_IdentityHash", "SlackSocketDiscardReceipts", "IdentityHash", schema: schema.Schema, unique: true);
        migrationBuilder.CreateIndex("IX_SlackSocketDiscardReceipts_SubscriptionId", "SlackSocketDiscardReceipts", "SubscriptionId", schema: schema.Schema);
        migrationBuilder.CreateIndex("IX_SlackSocketDiscardReceipts_TenantId_EnvironmentId_Id", "SlackSocketDiscardReceipts", new[] { "TenantId", "EnvironmentId", "Id" }, schema: schema.Schema);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Discard identity history cannot be safely erased by downgrade.");

    protected override void BuildTargetModel(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) => InitialSlackSocketReceiptModel.Build(modelBuilder);
}

using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Migrations;

[DbContext(typeof(AdmissionElsaDbContext))]
[Migration("20261008040000_InitialAdmission")]
public sealed class InitialAdmission(IElsaDbContextSchema schema) : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(schema.Schema);
        migrationBuilder.CreateTable(name: "AdmissionSubscriptions", schema: schema.Schema, columns: table => new
        {
            Id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            ConfigurationJson = table.Column<string>(type: "text", nullable: false),
            ConfigurationFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            Revision = table.Column<long>(type: "bigint", nullable: false),
            ActivationEpoch = table.Column<long>(type: "bigint", nullable: false),
            Active = table.Column<bool>(type: "boolean", nullable: false),
            Retired = table.Column<bool>(type: "boolean", nullable: false),
            BootstrapVerified = table.Column<bool>(type: "boolean", nullable: false),
            ActiveReservations = table.Column<int>(type: "integer", nullable: false),
            RetainedRecords = table.Column<int>(type: "integer", nullable: false),
            ReconciliationCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            TenantId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            EnvironmentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AdmissionSubscriptions", x => x.Id);
            table.CheckConstraint("CK_AdmissionSubscriptions_Capacity", "\"ActiveReservations\" >= 0 AND \"RetainedRecords\" >= \"ActiveReservations\"");
        });
        migrationBuilder.CreateTable(name: "Admissions", schema: schema.Schema, columns: table => new
        {
            Id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            SubscriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            IdentityHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
            ProviderEventId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
            Payload = table.Column<string>(type: "text", nullable: true),
            PayloadFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            EventFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            AdmittedConfigurationJson = table.Column<string>(type: "text", nullable: false),
            ConfigurationFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            ActivationEpoch = table.Column<long>(type: "bigint", nullable: false),
            AdmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            EventOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            Revision = table.Column<long>(type: "bigint", nullable: false),
            State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            WorkflowInstanceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            AttemptId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            AuthorityOutstanding = table.Column<bool>(type: "boolean", nullable: false),
            CheckpointFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
            BookmarkIdsJson = table.Column<string>(type: "text", nullable: true),
            RecoveryCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            AuditReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            TerminalDisposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
            TerminalAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            ActiveReservationReleased = table.Column<bool>(type: "boolean", nullable: false),
            RetainedRecordReleased = table.Column<bool>(type: "boolean", nullable: false),
            TenantId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            EnvironmentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Admissions", x => x.Id);
            table.ForeignKey("FK_Admissions_AdmissionSubscriptions_SubscriptionId", x => x.SubscriptionId, principalTable: "AdmissionSubscriptions", principalColumn: "Id", principalSchema: schema.Schema, onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_AdmissionSubscriptions_TenantId_EnvironmentId_Id", "AdmissionSubscriptions", new[] { "TenantId", "EnvironmentId", "Id" }, schema: schema.Schema, unique: false);
        migrationBuilder.CreateIndex("IX_Admissions_IdentityHash", "Admissions", new[] { "IdentityHash" }, schema: schema.Schema, unique: true);
        migrationBuilder.CreateIndex("IX_Admissions_WorkflowInstanceId", "Admissions", new[] { "WorkflowInstanceId" }, schema: schema.Schema, unique: true);
        migrationBuilder.CreateIndex("IX_Admissions_TenantId_EnvironmentId_Id", "Admissions", new[] { "TenantId", "EnvironmentId", "Id" }, schema: schema.Schema, unique: false, filter: "\"State\" <> 'Terminal'");
        migrationBuilder.CreateIndex("IX_Admissions_SubscriptionId_State_TerminalAt_Id", "Admissions", new[] { "SubscriptionId", "State", "TerminalAt", "Id" }, schema: schema.Schema, unique: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Admission ownership and deduplication history cannot be safely discarded by downgrade.");

    protected override void BuildTargetModel(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) => InitialAdmissionModel.Build(modelBuilder);
}

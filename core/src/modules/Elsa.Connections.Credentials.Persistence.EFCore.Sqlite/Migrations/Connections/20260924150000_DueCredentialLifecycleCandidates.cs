using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Migrations.Connections;

public sealed partial class DueCredentialLifecycleCandidates : Migration
{
    private readonly IElsaDbContextSchema _schema;

    public DueCredentialLifecycleCandidates(IElsaDbContextSchema schema) => _schema = schema ?? throw new ArgumentNullException(nameof(schema));

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "CredentialExpiresAt", table: "Connections", type: "TEXT", nullable: true, schema: _schema.Schema);
        migrationBuilder.AddColumn<string>(name: "CredentialKind", table: "Connections", type: "TEXT", maxLength: 24, nullable: true, schema: _schema.Schema);
        migrationBuilder.AddColumn<string>(name: "StagedCredentialExpiresAt", table: "Connections", type: "TEXT", nullable: true, schema: _schema.Schema);
        migrationBuilder.AddColumn<string>(name: "StagedCredentialKind", table: "Connections", type: "TEXT", maxLength: 24, nullable: true, schema: _schema.Schema);

        migrationBuilder.CreateIndex(name: "IX_Conn_Due", schema: _schema.Schema,
            table: "Connections", columns: new[] { "TenantId", "EnvironmentId", "CredentialKind", "CredentialExpiresAt", "Id" });
        migrationBuilder.CreateIndex(name: "IX_Conn_Lease", schema: _schema.Schema,
            table: "Connections", columns: new[] { "TenantId", "EnvironmentId", "Status", "OperationStatus", "OperationLeaseExpiresAt", "Id" });
        migrationBuilder.CreateIndex(name: "IX_Cleanup_Due", schema: _schema.Schema,
            table: "ConnectionGenerationCleanups", columns: new[] { "TenantId", "EnvironmentId", "Status", "LeaseExpiresAt", "ConnectionId", "GenerationId" });
        migrationBuilder.CreateIndex(name: "IX_Offboarding_Retry", schema: _schema.Schema,
            table: "ConnectionOffboardingOperations", columns: new[] { "TenantId", "EnvironmentId", "Status", "NextAttemptAt", "ConnectionId", "Id" });
        migrationBuilder.CreateIndex(name: "IX_Offboarding_Lease", schema: _schema.Schema,
            table: "ConnectionOffboardingOperations", columns: new[] { "TenantId", "EnvironmentId", "Status", "LeaseExpiresAt", "ConnectionId", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("This migration is irreversible because removing due-state metadata would disable safe credential lifecycle scheduling.");
}

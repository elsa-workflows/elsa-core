using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Migrations.Connections;

public partial class ConnectionOffboardingOperations : Migration
{
    private readonly IElsaDbContextSchema _schema;

    public ConnectionOffboardingOperations(IElsaDbContextSchema schema) => _schema = schema ?? throw new ArgumentNullException(nameof(schema));

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConnectionOffboardingOperations",
            schema: _schema.Schema,
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                TenantId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                EnvironmentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ConnectionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ProviderId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ProviderAccountId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                GenerationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                Fence = table.Column<long>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                NextAttemptAt = table.Column<string>(type: "TEXT", nullable: true),
                LeaseExpiresAt = table.Column<string>(type: "TEXT", nullable: true),
                LastSafeErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ConnectionOffboardingOperations", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_ConnectionOffboardingOperations_TenantId_EnvironmentId_ConnectionId_Status_CreatedAt",
            schema: _schema.Schema,
            table: "ConnectionOffboardingOperations",
            columns: new[] { "TenantId", "EnvironmentId", "ConnectionId", "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ConnectionOffboardingOperations_TenantId_EnvironmentId_ConnectionId_GenerationId_Status",
            schema: _schema.Schema,
            table: "ConnectionOffboardingOperations",
            columns: new[] { "TenantId", "EnvironmentId", "ConnectionId", "GenerationId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("This migration is irreversible because dropping offboarding history could permit unresolved remote operations to be lost.");
}

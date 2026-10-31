using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Migrations.Connections;

/// <inheritdoc />
public partial class GenerationCleanupLedger : Migration
{
    private readonly IElsaDbContextSchema _schema;

    public GenerationCleanupLedger(IElsaDbContextSchema schema) => _schema = schema ?? throw new ArgumentNullException(nameof(schema));

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConnectionGenerationCleanups",
            schema: _schema.Schema,
            columns: table => new
            {
                ConnectionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                GenerationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                TenantId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                EnvironmentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                Fence = table.Column<long>(type: "INTEGER", nullable: false),
                LeaseExpiresAt = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ConnectionGenerationCleanups", x => new { x.ConnectionId, x.GenerationId }));

        migrationBuilder.CreateIndex(
            name: "IX_ConnectionGenerationCleanups_TenantId_EnvironmentId_ConnectionId",
            schema: _schema.Schema,
            table: "ConnectionGenerationCleanups",
            columns: new[] { "TenantId", "EnvironmentId", "ConnectionId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("This migration is irreversible because dropping the cleanup tombstones could permit a retired credential generation to be reused.");
}

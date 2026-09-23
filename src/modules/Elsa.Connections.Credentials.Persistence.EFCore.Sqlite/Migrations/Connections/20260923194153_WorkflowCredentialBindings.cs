using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Migrations.Connections;

/// <inheritdoc />
public partial class WorkflowCredentialBindings : Migration
{
    private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

    public WorkflowCredentialBindings(Elsa.Persistence.EFCore.IElsaDbContextSchema schema) => _schema = schema ?? throw new ArgumentNullException(nameof(schema));

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConnectionCredentialBindings",
            schema: _schema.Schema,
            columns: table => new
            {
                TenantId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                EnvironmentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                LogicalBindingId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ConnectionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Revision = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConnectionCredentialBindings", x => new { x.TenantId, x.EnvironmentId, x.LogicalBindingId });
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("This migration is irreversible because dropping logical workflow bindings would prevent persisted workflows from resuming in their configured environment.");
}

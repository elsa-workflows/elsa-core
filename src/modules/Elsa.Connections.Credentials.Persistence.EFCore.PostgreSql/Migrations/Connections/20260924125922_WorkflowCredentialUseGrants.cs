using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Migrations.Connections;

public partial class WorkflowCredentialUseGrants : Migration
{
    private readonly IElsaDbContextSchema _schema;

    public WorkflowCredentialUseGrants(IElsaDbContextSchema schema) => _schema = schema ?? throw new ArgumentNullException(nameof(schema));

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConnectionCredentialUseGrants",
            schema: _schema.Schema,
            columns: table => new
            {
                TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                WorkflowInstanceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                LogicalBindingId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ConnectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                BindingRevision = table.Column<long>(type: "bigint", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IssuedByActorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                WithdrawnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ConnectionCredentialUseGrants", x => new
            {
                x.TenantId, x.EnvironmentId, x.WorkflowInstanceId, x.LogicalBindingId
            }));
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("This migration is irreversible because dropping workflow-use grants would erase the authorization and withdrawal record.");
}

using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Migrations.Connections
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        public Initial(IElsaDbContextSchema schema) => _schema = schema ?? throw new ArgumentNullException(nameof(schema));

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: _schema.Schema);

            migrationBuilder.CreateTable(
                name: "Connections",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EnvironmentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProviderAccountId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrentSecretName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CurrentGenerationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OperationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OperationExpectedRevision = table.Column<long>(type: "INTEGER", nullable: false),
                    OperationFence = table.Column<long>(type: "INTEGER", nullable: false),
                    OperationLeaseExpiresAt = table.Column<string>(type: "TEXT", nullable: true),
                    OperationStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OperationSourceGenerationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PlannedSecretName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PlannedGenerationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StagedSecretName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StagedGenerationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastSafeErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TenantId_EnvironmentId_Id",
                schema: _schema.Schema,
                table: "Connections",
                columns: new[] { "TenantId", "EnvironmentId", "Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration is irreversible because dropping connection lifecycle state could orphan managed credential generations.");
        }
    }
}

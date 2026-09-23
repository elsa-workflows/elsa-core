using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Migrations.Connections
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "ConnectionCredentialBindings",
                schema: "Elsa",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogicalBindingId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionCredentialBindings", x => new { x.TenantId, x.EnvironmentId, x.LogicalBindingId });
                });

            migrationBuilder.CreateTable(
                name: "ConnectionGenerationCleanups",
                schema: "Elsa",
                columns: table => new
                {
                    ConnectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GenerationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Fence = table.Column<long>(type: "bigint", nullable: false),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionGenerationCleanups", x => new { x.ConnectionId, x.GenerationId });
                });

            migrationBuilder.CreateTable(
                name: "ConnectionOffboardingOperations",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GenerationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Fence = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSafeErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionOffboardingOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Connections",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    CurrentSecretName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CurrentGenerationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperationExpectedRevision = table.Column<long>(type: "bigint", nullable: false),
                    OperationFence = table.Column<long>(type: "bigint", nullable: false),
                    OperationLeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OperationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OperationSourceGenerationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PlannedSecretName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PlannedGenerationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StagedSecretName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StagedGenerationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastSafeErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionGenerationCleanups_TenantId_EnvironmentId_Connect~",
                schema: "Elsa",
                table: "ConnectionGenerationCleanups",
                columns: new[] { "TenantId", "EnvironmentId", "ConnectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionOffboardingOperations_TenantId_EnvironmentId_Con~1",
                schema: "Elsa",
                table: "ConnectionOffboardingOperations",
                columns: new[] { "TenantId", "EnvironmentId", "ConnectionId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionOffboardingOperations_TenantId_EnvironmentId_Conn~",
                schema: "Elsa",
                table: "ConnectionOffboardingOperations",
                columns: new[] { "TenantId", "EnvironmentId", "ConnectionId", "GenerationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TenantId_EnvironmentId_Id",
                schema: "Elsa",
                table: "Connections",
                columns: new[] { "TenantId", "EnvironmentId", "Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration is irreversible because dropping connection lifecycle state could orphan managed credential generations or lose unresolved offboarding operations.");
        }
    }
}

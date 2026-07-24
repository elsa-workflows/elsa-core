using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Elsa.Persistence.EFCore.PostgreSql.Migrations.Identity
{
    /// <inheritdoc />
    public partial class ExternalAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HashedPasswordSalt",
                schema: "Elsa",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationAuthorizationGrants",
                schema: "Elsa",
                columns: table => new
                {
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    CallbackUri = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "text", nullable: true),
                    PkceChallenge = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationAuthorizationGrants", x => x.CodeHash);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationBrokerTransactions",
                schema: "Elsa",
                columns: table => new
                {
                    HandleHash = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    CallbackUri = table.Column<string>(type: "text", nullable: false),
                    ReturnPath = table.Column<string>(type: "text", nullable: false),
                    ClientState = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "text", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "text", nullable: true),
                    PkceChallenge = table.Column<string>(type: "text", nullable: false),
                    ProviderNonce = table.Column<string>(type: "text", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "bytea", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationBrokerTransactions", x => new { x.Purpose, x.HandleHash });
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationClients",
                schema: "Elsa",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    ClientType = table.Column<int>(type: "integer", nullable: false),
                    CallbackUrisJson = table.Column<string>(type: "text", nullable: false),
                    LogoutCallbackUrisJson = table.Column<string>(type: "text", nullable: false),
                    AllowedOriginsJson = table.Column<string>(type: "text", nullable: false),
                    AllowedReturnPathPrefixesJson = table.Column<string>(type: "text", nullable: false),
                    SecretBindingJson = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationClients", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationConnectionObservations",
                schema: "Elsa",
                columns: table => new
                {
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    TestedMaterialRevision = table.Column<string>(type: "text", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    DurationTicks = table.Column<long>(type: "bigint", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    WarningsJson = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationConnectionObservations", x => x.ConnectionId);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationPreviewResults",
                schema: "Elsa",
                columns: table => new
                {
                    HandleHash = table.Column<string>(type: "text", nullable: false),
                    AdministratorId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    MaterialRevision = table.Column<string>(type: "text", nullable: false),
                    Issuer = table.Column<string>(type: "text", nullable: false),
                    MaskedSubject = table.Column<string>(type: "text", nullable: false),
                    ProjectedClaimsJson = table.Column<string>(type: "text", nullable: false),
                    PolicyDecision = table.Column<string>(type: "text", nullable: false),
                    PermissionProjectionJson = table.Column<string>(type: "text", nullable: false),
                    WarningsJson = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationPreviewResults", x => x.HandleHash);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationRegistryVersions",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationRegistryVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationSessions",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    ConnectionMaterialRevision = table.Column<string>(type: "text", nullable: false),
                    SecretGenerationFingerprint = table.Column<string>(type: "text", nullable: true),
                    Issuer = table.Column<string>(type: "text", nullable: false),
                    SubjectHash = table.Column<string>(type: "text", nullable: false),
                    ExternalGrantsJson = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RefreshExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentRefreshTokenHash = table.Column<string>(type: "text", nullable: false),
                    RefreshGeneration = table.Column<long>(type: "bigint", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIdentityLinks",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    Issuer = table.Column<string>(type: "text", nullable: false),
                    SubjectHash = table.Column<string>(type: "text", nullable: false),
                    SubjectHint = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSignedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIdentityLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalIdentityLinks_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Elsa",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviderConnections",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    AdapterType = table.Column<string>(type: "text", nullable: false),
                    AdapterSettingsVersion = table.Column<int>(type: "integer", nullable: false),
                    AdapterSettingsJson = table.Column<string>(type: "text", nullable: false),
                    SecretBindingsJson = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IconId = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnlinkedPolicyJson = table.Column<string>(type: "text", nullable: true),
                    PermissionGrantSourcesJson = table.Column<string>(type: "text", nullable: false),
                    ClaimProjectionJson = table.Column<string>(type: "text", nullable: false),
                    UpstreamLogoutMode = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    MaterialRevision = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviderConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationAuthorizationGrant_ExpiresAt",
                schema: "Elsa",
                table: "ExternalAuthenticationAuthorizationGrants",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationBrokerTransaction_ExpiresAt",
                schema: "Elsa",
                table: "ExternalAuthenticationBrokerTransactions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationPreviewResult_ExpiresAt",
                schema: "Elsa",
                table: "ExternalAuthenticationPreviewResults",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationSession_ConnectionId",
                schema: "Elsa",
                table: "ExternalAuthenticationSessions",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationSession_RefreshTokenHash",
                schema: "Elsa",
                table: "ExternalAuthenticationSessions",
                column: "CurrentRefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationSession_TenantId_UserId",
                schema: "Elsa",
                table: "ExternalAuthenticationSessions",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLink_Identity",
                schema: "Elsa",
                table: "ExternalIdentityLinks",
                columns: new[] { "TenantId", "ConnectionId", "Issuer", "SubjectHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLink_TenantId_UserId",
                schema: "Elsa",
                table: "ExternalIdentityLinks",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLinks_UserId",
                schema: "Elsa",
                table: "ExternalIdentityLinks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderConnection_MaterialRevision",
                schema: "Elsa",
                table: "IdentityProviderConnections",
                column: "MaterialRevision");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderConnection_TenantId_Key",
                schema: "Elsa",
                table: "IdentityProviderConnections",
                columns: new[] { "TenantId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalAuthenticationAuthorizationGrants",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationBrokerTransactions",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationClients",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationConnectionObservations",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationPreviewResults",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationRegistryVersions",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationSessions",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "ExternalIdentityLinks",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "IdentityProviderConnections",
                schema: "Elsa");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPasswordSalt",
                schema: "Elsa",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}

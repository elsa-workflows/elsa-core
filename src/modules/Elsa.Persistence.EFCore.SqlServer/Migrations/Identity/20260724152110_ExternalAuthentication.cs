using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Identity
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
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationAuthorizationGrants",
                schema: "Elsa",
                columns: table => new
                {
                    CodeHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CallbackUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PkceChallenge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
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
                    HandleHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CallbackUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReturnPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PkceChallenge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderNonce = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
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
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientType = table.Column<int>(type: "int", nullable: false),
                    CallbackUrisJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoutCallbackUrisJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedOriginsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedReturnPathPrefixesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretBindingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
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
                    ConnectionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TestedMaterialRevision = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationTicks = table.Column<long>(type: "bigint", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    HandleHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdministratorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaterialRevision = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaskedSubject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectedClaimsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PolicyDecision = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PermissionProjectionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectionMaterialRevision = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretGenerationFingerprint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Issuer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalGrantsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RefreshExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CurrentRefreshTokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RefreshGeneration = table.Column<long>(type: "bigint", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubjectHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubjectHint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSignedInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
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
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdapterType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdapterSettingsVersion = table.Column<int>(type: "int", nullable: false),
                    AdapterSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretBindingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnlinkedPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PermissionGrantSourcesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClaimProjectionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpstreamLogoutMode = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    MaterialRevision = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}

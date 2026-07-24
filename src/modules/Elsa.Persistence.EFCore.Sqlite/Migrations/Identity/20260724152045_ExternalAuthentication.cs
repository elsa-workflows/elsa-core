using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Identity
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
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationAuthorizationGrants",
                schema: "Elsa",
                columns: table => new
                {
                    CodeHash = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    CallbackUri = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    PkceChallenge = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    ConsumedAt = table.Column<string>(type: "TEXT", nullable: true)
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
                    HandleHash = table.Column<string>(type: "TEXT", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    CallbackUri = table.Column<string>(type: "TEXT", nullable: false),
                    ReturnPath = table.Column<string>(type: "TEXT", nullable: false),
                    ClientState = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionId = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "TEXT", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "TEXT", nullable: true),
                    PkceChallenge = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderNonce = table.Column<string>(type: "TEXT", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    ConsumedAt = table.Column<string>(type: "TEXT", nullable: true)
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
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ClientType = table.Column<int>(type: "INTEGER", nullable: false),
                    CallbackUrisJson = table.Column<string>(type: "TEXT", nullable: false),
                    LogoutCallbackUrisJson = table.Column<string>(type: "TEXT", nullable: false),
                    AllowedOriginsJson = table.Column<string>(type: "TEXT", nullable: false),
                    AllowedReturnPathPrefixesJson = table.Column<string>(type: "TEXT", nullable: false),
                    SecretBindingJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
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
                    ConnectionId = table.Column<string>(type: "TEXT", nullable: false),
                    TestedMaterialRevision = table.Column<string>(type: "TEXT", nullable: false),
                    ObservedAt = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    DurationTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    WarningsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false)
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
                    HandleHash = table.Column<string>(type: "TEXT", nullable: false),
                    AdministratorId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionId = table.Column<string>(type: "TEXT", nullable: false),
                    MaterialRevision = table.Column<string>(type: "TEXT", nullable: false),
                    Issuer = table.Column<string>(type: "TEXT", nullable: false),
                    MaskedSubject = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectedClaimsJson = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyDecision = table.Column<string>(type: "TEXT", nullable: false),
                    PermissionProjectionJson = table.Column<string>(type: "TEXT", nullable: false),
                    WarningsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    ConsumedAt = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionMaterialRevision = table.Column<string>(type: "TEXT", nullable: false),
                    SecretGenerationFingerprint = table.Column<string>(type: "TEXT", nullable: true),
                    Issuer = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectHash = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalGrantsJson = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<string>(type: "TEXT", nullable: false),
                    LastRefreshedAt = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentRefreshTokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshGeneration = table.Column<long>(type: "INTEGER", nullable: false),
                    RevokedAt = table.Column<string>(type: "TEXT", nullable: true),
                    RevocationReason = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionId = table.Column<string>(type: "TEXT", nullable: false),
                    Issuer = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectHash = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectHint = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    LastSignedInAt = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    AdapterType = table.Column<string>(type: "TEXT", nullable: false),
                    AdapterSettingsVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    AdapterSettingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SecretBindingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    IconId = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAt = table.Column<string>(type: "TEXT", nullable: true),
                    UnlinkedPolicyJson = table.Column<string>(type: "TEXT", nullable: true),
                    PermissionGrantSourcesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimProjectionJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpstreamLogoutMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false),
                    MaterialRevision = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
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
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

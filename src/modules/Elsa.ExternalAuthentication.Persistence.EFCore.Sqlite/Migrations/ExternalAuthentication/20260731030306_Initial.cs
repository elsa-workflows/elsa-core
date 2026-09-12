using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.Migrations.ExternalAuthentication
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public Initial(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationAuthorizationGrants",
                schema: _schema.Schema,
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
                    ExpiresAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    ConsumedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationAuthorizationGrants", x => x.CodeHash);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationBrokerTransactions",
                schema: _schema.Schema,
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
                    ConnectionKey = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "TEXT", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "TEXT", nullable: true),
                    PkceChallenge = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderNonce = table.Column<string>(type: "TEXT", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    ConsumedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationBrokerTransactions", x => new { x.Purpose, x.HandleHash });
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationConnectionObservations",
                schema: _schema.Schema,
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
                schema: _schema.Schema,
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
                    ExpiresAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    ConsumedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationPreviewResults", x => x.HandleHash);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationRegistryVersions",
                schema: _schema.Schema,
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
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionKey = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionMaterialRevision = table.Column<string>(type: "TEXT", nullable: false),
                    SecretGenerationFingerprint = table.Column<string>(type: "TEXT", nullable: true),
                    Issuer = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectHash = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalGrantsJson = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<string>(type: "TEXT", nullable: false),
                    LastRefreshedAt = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshGeneration = table.Column<long>(type: "INTEGER", nullable: false),
                    RevokedAt = table.Column<string>(type: "TEXT", nullable: true),
                    RevocationReason = table.Column<string>(type: "TEXT", nullable: true),
                    ProtectedUpstreamLogoutHint = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationSessionRefreshTokens",
                schema: _schema.Schema,
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationSessionRefreshTokens", x => x.SessionId);
                    table.ForeignKey("FK_ExternalAuthenticationSessionRefreshTokens_ExternalAuthenticationSessions_SessionId", x => x.SessionId, principalSchema: _schema.Schema, principalTable: "ExternalAuthenticationSessions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIdentityLinks",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionKey = table.Column<string>(type: "TEXT", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviderConnections",
                schema: _schema.Schema,
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
                    IsPreferred = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverridesConfigurationConnection = table.Column<bool>(type: "INTEGER", nullable: false),
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
                schema: _schema.Schema,
                table: "ExternalAuthenticationAuthorizationGrants",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationBrokerTransaction_ExpiresAt",
                schema: _schema.Schema,
                table: "ExternalAuthenticationBrokerTransactions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationPreviewResult_ExpiresAt",
                schema: _schema.Schema,
                table: "ExternalAuthenticationPreviewResults",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationSession_ConnectionKey",
                schema: _schema.Schema,
                table: "ExternalAuthenticationSessions",
                column: "ConnectionKey");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationSessionRefreshToken_Hash",
                schema: _schema.Schema,
                table: "ExternalAuthenticationSessionRefreshTokens",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationSession_TenantId_UserId",
                schema: _schema.Schema,
                table: "ExternalAuthenticationSessions",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLink_Identity",
                schema: _schema.Schema,
                table: "ExternalIdentityLinks",
                columns: new[] { "TenantId", "ConnectionKey", "Issuer", "SubjectHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLink_TenantId_UserId",
                schema: _schema.Schema,
                table: "ExternalIdentityLinks",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderConnection_MaterialRevision",
                schema: _schema.Schema,
                table: "IdentityProviderConnections",
                column: "MaterialRevision");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderConnection_TenantId_Key",
                schema: _schema.Schema,
                table: "IdentityProviderConnections",
                columns: new[] { "TenantId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalAuthenticationAuthorizationGrants",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationBrokerTransactions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationConnectionObservations",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationPreviewResults",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationRegistryVersions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationSessionRefreshTokens",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationSessions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ExternalIdentityLinks",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "IdentityProviderConnections",
                schema: _schema.Schema);
        }
    }
}

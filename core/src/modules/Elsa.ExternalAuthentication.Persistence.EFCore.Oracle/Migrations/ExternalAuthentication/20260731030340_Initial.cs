using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Oracle.Migrations.ExternalAuthentication
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
                    CodeHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClientId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CallbackUri = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    UserId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PkceChallenge = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
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
                    HandleHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Purpose = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClientId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CallbackUri = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ReturnPath = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ClientState = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ConnectionKey = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PkceChallenge = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ProviderNonce = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
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
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    TestedMaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Category = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DurationTicks = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Summary = table.Column<string>(type: "NCLOB", nullable: false),
                    WarningsJson = table.Column<string>(type: "NCLOB", nullable: false),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
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
                    HandleHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AdministratorId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    MaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Issuer = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    MaskedSubject = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ProjectedClaimsJson = table.Column<string>(type: "NCLOB", nullable: false),
                    PolicyDecision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PermissionProjectionJson = table.Column<string>(type: "NCLOB", nullable: false),
                    WarningsJson = table.Column<string>(type: "NCLOB", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Version = table.Column<long>(type: "NUMBER(19)", nullable: false)
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
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ConnectionKey = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ConnectionMaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SecretGenerationFingerprint = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Issuer = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SubjectHash = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ExternalGrantsJson = table.Column<string>(type: "NCLOB", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    RefreshExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    RefreshGeneration = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    RevocationReason = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
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
                    SessionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Hash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
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
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ConnectionKey = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Issuer = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    SubjectHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    SubjectHint = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    LastSignedInAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
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
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Key = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AdapterType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AdapterSettingsVersion = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdapterSettingsJson = table.Column<string>(type: "NCLOB", nullable: false),
                    SecretBindingsJson = table.Column<string>(type: "NCLOB", nullable: false),
                    DisplayName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    IconId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IsPreferred = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    IsEnabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    OverridesConfigurationConnection = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    UnlinkedPolicyJson = table.Column<string>(type: "NCLOB", nullable: true),
                    PermissionGrantSourcesJson = table.Column<string>(type: "NCLOB", nullable: false),
                    ClaimProjectionJson = table.Column<string>(type: "NCLOB", nullable: false),
                    UpstreamLogoutMode = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Revision = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    MaterialRevision = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
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

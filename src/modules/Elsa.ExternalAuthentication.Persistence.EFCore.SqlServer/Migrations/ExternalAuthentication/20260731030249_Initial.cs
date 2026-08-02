using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.ExternalAuthentication.Persistence.EFCore.SqlServer.Migrations.ExternalAuthentication
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
                    CodeHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CallbackUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PkceChallenge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
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
                    HandleHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CallbackUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReturnPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PkceChallenge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderNonce = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
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
                schema: _schema.Schema,
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
                    ExpiresAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
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
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectionKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                    RevocationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProtectedUpstreamLogoutHint = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIdentityLinks",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectionKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviderConnections",
                schema: _schema.Schema,
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
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    OverridesConfigurationConnection = table.Column<bool>(type: "bit", nullable: false),
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
                name: "IX_ExternalAuthenticationSession_RefreshTokenHash",
                schema: _schema.Schema,
                table: "ExternalAuthenticationSessions",
                column: "CurrentRefreshTokenHash",
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

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Elsa.ExternalAuthentication.Persistence.EFCore.PostgreSql.Migrations.ExternalAuthentication
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
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    CallbackUri = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "text", nullable: true),
                    PkceChallenge = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                    HandleHash = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    CallbackUri = table.Column<string>(type: "text", nullable: false),
                    ReturnPath = table.Column<string>(type: "text", nullable: false),
                    ClientState = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: true),
                    ConnectionKey = table.Column<string>(type: "text", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "text", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "text", nullable: true),
                    PkceChallenge = table.Column<string>(type: "text", nullable: false),
                    ProviderNonce = table.Column<string>(type: "text", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "bytea", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                schema: _schema.Schema,
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
                    ExpiresAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConnectionKey = table.Column<string>(type: "text", nullable: false),
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
                    RevocationReason = table.Column<string>(type: "text", nullable: true),
                    ProtectedUpstreamLogoutHint = table.Column<byte[]>(type: "bytea", nullable: true)
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
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ConnectionKey = table.Column<string>(type: "text", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviderConnections",
                schema: _schema.Schema,
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
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    OverridesConfigurationConnection = table.Column<bool>(type: "boolean", nullable: false),
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

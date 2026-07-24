using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Identity
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
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationAuthorizationGrants",
                schema: "Elsa",
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
                    ConsumedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
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
                    HandleHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Purpose = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClientId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CallbackUri = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ReturnPath = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ClientState = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ConnectionMaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SecretGenerationFingerprint = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PkceChallenge = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ProviderNonce = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ProtectedPayload = table.Column<byte[]>(type: "RAW(2000)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
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
                    ClientId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ClientType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CallbackUrisJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    LogoutCallbackUrisJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AllowedOriginsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AllowedReturnPathPrefixesJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SecretBindingJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "BOOLEAN", nullable: false)
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
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    TestedMaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Category = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DurationTicks = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Summary = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    WarningsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
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
                    HandleHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AdministratorId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    MaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Issuer = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    MaskedSubject = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ProjectedClaimsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PolicyDecision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PermissionProjectionJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    WarningsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
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
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AuthenticationClientId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ConnectionMaterialRevision = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SecretGenerationFingerprint = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Issuer = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SubjectHash = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ExternalGrantsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    RefreshExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    CurrentRefreshTokenHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    RefreshGeneration = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    RevocationReason = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
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
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ConnectionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
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
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Key = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AdapterType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AdapterSettingsVersion = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdapterSettingsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SecretBindingsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DisplayName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    IconId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IsDefault = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    IsEnabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    UnlinkedPolicyJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PermissionGrantSourcesJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ClaimProjectionJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
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
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: "Elsa",
                table: "Users",
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);
        }
    }
}

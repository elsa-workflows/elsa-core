using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.PostgreSql.Migrations.Identity
{
    /// <inheritdoc />
    public partial class PerTenantIdentityUniqueness : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public PerTenantIdentityUniqueness(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Name",
                schema: _schema.Schema,
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Role_Name",
                schema: _schema.Schema,
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Application_ClientId",
                schema: _schema.Schema,
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Application_Name",
                schema: _schema.Schema,
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_Name",
                schema: _schema.Schema,
                table: "Users",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId_Name",
                schema: _schema.Schema,
                table: "Roles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_TenantId_ClientId",
                schema: _schema.Schema,
                table: "Applications",
                columns: new[] { "TenantId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_TenantId_Name",
                schema: _schema.Schema,
                table: "Applications",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_TenantId_Name",
                schema: _schema.Schema,
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Role_TenantId_Name",
                schema: _schema.Schema,
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Application_TenantId_ClientId",
                schema: _schema.Schema,
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Application_TenantId_Name",
                schema: _schema.Schema,
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_User_Name",
                schema: _schema.Schema,
                table: "Users",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name",
                schema: _schema.Schema,
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_ClientId",
                schema: _schema.Schema,
                table: "Applications",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_Name",
                schema: _schema.Schema,
                table: "Applications",
                column: "Name",
                unique: true);
        }
    }
}
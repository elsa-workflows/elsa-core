using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.SqlServer.Migrations.Identity
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Roles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Applications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId",
                schema: _schema.Schema,
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId",
                schema: _schema.Schema,
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Application_TenantId",
                schema: _schema.Schema,
                table: "Applications",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_TenantId",
                schema: _schema.Schema,
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Role_TenantId",
                schema: _schema.Schema,
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Application_TenantId",
                schema: _schema.Schema,
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Applications");
        }
    }
}

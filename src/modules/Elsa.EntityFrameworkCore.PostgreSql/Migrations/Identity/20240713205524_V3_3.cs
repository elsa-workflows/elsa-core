using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Identity
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
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Roles",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "Applications",
                newName: "Applications",
                newSchema: "Elsa");

            migrationBuilder.AlterColumn<string>(
                name: "Roles",
                schema: _schema.Schema,
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPasswordSalt",
                schema: _schema.Schema,
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                schema: _schema.Schema,
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                schema: _schema.Schema,
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Roles",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Roles",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HashedClientSecretSalt",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HashedClientSecret",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HashedApiKeySalt",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HashedApiKey",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Applications",
                type: "text",
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

            migrationBuilder.RenameTable(
                name: "Users",
                schema: _schema.Schema,
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: _schema.Schema,
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "Applications",
                schema: _schema.Schema,
                newName: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "Roles",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPasswordSalt",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HashedPassword",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                table: "Roles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Roles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Roles",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HashedClientSecretSalt",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HashedClientSecret",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HashedApiKeySalt",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HashedApiKey",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Oracle.Migrations.Identity
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

            migrationBuilder.CreateTable(
                name: "Applications",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClientId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    HashedClientSecret = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    HashedClientSecretSalt = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    HashedApiKey = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    HashedApiKeySalt = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Roles = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Permissions = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    HashedPassword = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    HashedPasswordSalt = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Roles = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Application_TenantId",
                schema: _schema.Schema,
                table: "Applications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name",
                schema: _schema.Schema,
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId",
                schema: _schema.Schema,
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Name",
                schema: _schema.Schema,
                table: "Users",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId",
                schema: _schema.Schema,
                table: "Users",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Applications",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "Roles",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "Users",
                schema: _schema.Schema);
        }
    }
}

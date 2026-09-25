using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Agents.Persistence.EFCore.MySql.Migrations
{
    /// <inheritdoc />
    public partial class V3_6 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_6(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeysDefinitions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ServicesDefinitions",
                schema: _schema.Schema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeysDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeysDefinitions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServicesDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Settings = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesDefinitions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyDefinition_Name",
                schema: _schema.Schema,
                table: "ApiKeysDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyDefinition_TenantId",
                schema: _schema.Schema,
                table: "ApiKeysDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinition_Name",
                schema: _schema.Schema,
                table: "ServicesDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinition_TenantId",
                schema: _schema.Schema,
                table: "ServicesDefinitions",
                column: "TenantId");
        }
    }
}

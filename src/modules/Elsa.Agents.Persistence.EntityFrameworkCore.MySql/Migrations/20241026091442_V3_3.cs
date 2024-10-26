using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Agents.Persistence.EntityFrameworkCore.MySql.Migrations
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AgentDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentConfig = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitions", x => new { x.TenantId, x.Id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApiKeysDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeysDefinitions", x => new { x.TenantId, x.Id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServicesDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Settings = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesDefinitions", x => new { x.TenantId, x.Id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinition_Name",
                schema: _schema.Schema,
                table: "AgentDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinition_TenantId",
                schema: _schema.Schema,
                table: "AgentDefinitions",
                column: "TenantId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentDefinitions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ApiKeysDefinitions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ServicesDefinitions",
                schema: _schema.Schema);
        }
    }
}

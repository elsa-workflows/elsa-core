using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.MySql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class AddKeyValueStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeyValuePairs",
                schema: "Elsa",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerializedValue = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyValuePairs", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyValuePairs",
                schema: "Elsa");
        }
    }
}

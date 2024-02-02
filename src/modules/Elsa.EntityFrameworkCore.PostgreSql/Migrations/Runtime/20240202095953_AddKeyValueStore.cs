using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Runtime
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
                    Key = table.Column<string>(type: "text", nullable: false),
                    SerializedValue = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyValuePairs", x => x.Key);
                });
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

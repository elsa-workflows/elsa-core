using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerializedActivityStateCompressionAlgorithm",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KeyValuePairs",
                schema: "Elsa",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    SerializedValue = table.Column<string>(type: "text", nullable: false)
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

            migrationBuilder.DropColumn(
                name: "SerializedActivityStateCompressionAlgorithm",
                schema: "Elsa",
                table: "ActivityExecutionRecords");
        }
    }
}

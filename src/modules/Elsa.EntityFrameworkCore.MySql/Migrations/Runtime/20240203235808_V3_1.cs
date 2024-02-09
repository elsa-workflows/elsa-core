using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.MySql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V31 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerializedActivityStateCompressionAlgorithm",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerializedActivityStateCompressionAlgorithm",
                schema: "Elsa",
                table: "ActivityExecutionRecords");
        }
    }
}

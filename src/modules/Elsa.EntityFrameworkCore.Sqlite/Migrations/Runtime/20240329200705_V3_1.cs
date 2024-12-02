using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_1(IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerializedActivityStateCompressionAlgorithm",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerializedProperties",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KeyValuePairs",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    SerializedValue = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "KeyValuePairs");

            migrationBuilder.DropColumn(
                name: "SerializedActivityStateCompressionAlgorithm",
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "SerializedProperties",
                table: "ActivityExecutionRecords");
        }
    }
}

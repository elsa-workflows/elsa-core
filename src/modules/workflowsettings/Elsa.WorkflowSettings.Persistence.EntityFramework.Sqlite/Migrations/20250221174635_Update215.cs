using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Update215 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkflowBlueprintId",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkflowBlueprintId",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}

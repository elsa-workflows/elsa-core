using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.PostgreSql.Migrations
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
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkflowBlueprintId",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

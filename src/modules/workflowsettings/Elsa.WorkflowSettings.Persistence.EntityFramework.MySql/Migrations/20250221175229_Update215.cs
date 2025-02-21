using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Update215 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "Elsa",
                table: "WorkflowSettings",
                keyColumn: "WorkflowBlueprintId",
                keyValue: null,
                column: "WorkflowBlueprintId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowBlueprintId",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                schema: "Elsa",
                table: "WorkflowSettings",
                keyColumn: "Key",
                keyValue: null,
                column: "Key",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkflowBlueprintId",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Elsa",
                table: "WorkflowSettings",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

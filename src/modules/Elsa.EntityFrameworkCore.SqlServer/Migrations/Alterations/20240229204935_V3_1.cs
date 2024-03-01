using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.SqlServer.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SerializedWorkflowInstanceIds",
                schema: "Elsa",
                table: "AlterationPlans",
                newName: "SerializedWorkflowInstanceFilter");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SerializedWorkflowInstanceFilter",
                schema: "Elsa",
                table: "AlterationPlans",
                newName: "SerializedWorkflowInstanceIds");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}

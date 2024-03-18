using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Alterations
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
                newName: "TenantId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "SerializedWorkflowInstanceFilter",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_TenantId",
                schema: "Elsa",
                table: "AlterationPlans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_TenantId",
                schema: "Elsa",
                table: "AlterationJobs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlterationPlan_TenantId",
                schema: "Elsa",
                table: "AlterationPlans");

            migrationBuilder.DropIndex(
                name: "IX_AlterationJob_TenantId",
                schema: "Elsa",
                table: "AlterationJobs");

            migrationBuilder.DropColumn(
                name: "SerializedWorkflowInstanceFilter",
                schema: "Elsa",
                table: "AlterationPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "AlterationJobs");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "AlterationPlans",
                newName: "SerializedWorkflowInstanceIds");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

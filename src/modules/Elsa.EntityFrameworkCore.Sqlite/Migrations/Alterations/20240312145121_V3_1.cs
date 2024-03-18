using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SerializedWorkflowInstanceIds",
                table: "AlterationPlans",
                newName: "SerializedWorkflowInstanceFilter");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_TenantId",
                table: "AlterationPlans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_TenantId",
                table: "AlterationJobs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
<<<<<<<< HEAD:src/modules/Elsa.EntityFrameworkCore.Sqlite/Migrations/Alterations/20240310130545_V3_1.cs
            migrationBuilder.DropIndex(
                name: "IX_AlterationPlan_TenantId",
                table: "AlterationPlans");

            migrationBuilder.DropIndex(
                name: "IX_AlterationJob_TenantId",
                table: "AlterationJobs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AlterationPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AlterationJobs");
========
            migrationBuilder.RenameColumn(
                name: "SerializedWorkflowInstanceFilter",
                table: "AlterationPlans",
                newName: "SerializedWorkflowInstanceIds");
>>>>>>>> origin/main:src/modules/Elsa.EntityFrameworkCore.Sqlite/Migrations/Alterations/20240312145121_V3_1.cs

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AlterationPlans",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AlterationJobs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}

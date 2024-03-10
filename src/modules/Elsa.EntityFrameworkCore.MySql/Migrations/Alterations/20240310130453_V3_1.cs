using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.MySql.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "TenantId",
                schema: "Elsa",
                table: "AlterationPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "AlterationJobs");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationPlans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "Elsa",
                table: "AlterationJobs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

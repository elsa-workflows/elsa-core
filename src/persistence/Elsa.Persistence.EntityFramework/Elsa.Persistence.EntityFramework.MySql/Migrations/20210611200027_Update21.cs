using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.MySql.Migrations
{
    public partial class Update21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "WorkflowInstances",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LastExecutedActivityId",
                table: "WorkflowInstances",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OutputStorageProviderName",
                table: "WorkflowDefinitions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastExecutedActivityId",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "OutputStorageProviderName",
                table: "WorkflowDefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "WorkflowInstances",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

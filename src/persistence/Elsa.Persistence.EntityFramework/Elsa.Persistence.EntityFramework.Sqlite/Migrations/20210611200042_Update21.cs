using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.Sqlite.Migrations
{
    public partial class Update21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastExecutedActivityId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputStorageProviderName",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastExecutedActivityId",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "OutputStorageProviderName",
                schema: "Elsa",
                table: "WorkflowDefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}

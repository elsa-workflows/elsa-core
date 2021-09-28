using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.Oracle.Migrations
{
    public partial class Update23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastExecutedActivityId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB)(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelType",
                schema: "Elsa",
                table: "Bookmarks",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NCLOB");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                schema: "Elsa",
                table: "Bookmarks",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NCLOB");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastExecutedActivityId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NCLOB)(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelType",
                schema: "Elsa",
                table: "Bookmarks",
                type: "NCLOB",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                schema: "Elsa",
                table: "Bookmarks",
                type: "NCLOB",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");
        }
    }
}

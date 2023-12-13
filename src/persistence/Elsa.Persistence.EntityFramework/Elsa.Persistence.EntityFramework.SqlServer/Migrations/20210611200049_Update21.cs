using System;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.SqlServer.Migrations
{
    public partial class Update21 : Migration
    {
        private readonly IElsaDbContextSchema _schema;
        public Update21(IElsaDbContextSchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
            
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "CorrelationId");
            
            migrationBuilder.AddColumn<string>(
                name: "LastExecutedActivityId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputStorageProviderName",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastExecutedActivityId",
                schema: _schema.Schema,
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "OutputStorageProviderName",
                schema: _schema.Schema,
                table: "WorkflowDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances");
            
            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
            
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "CorrelationId");
        }
    }
}

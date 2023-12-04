using System;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.PostgreSql.Migrations
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
            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastExecutedActivityId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputStorageProviderName",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                type: "text",
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

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

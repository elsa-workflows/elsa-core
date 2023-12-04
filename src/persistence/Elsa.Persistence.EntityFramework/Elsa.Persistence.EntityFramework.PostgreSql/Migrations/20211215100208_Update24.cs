using System;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.PostgreSql.Migrations
{
    public partial class Update24 : Migration
    {
        private readonly IElsaDbContextSchema _schema;
        public Update24(IElsaDbContextSchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowInstances");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

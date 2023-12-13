using System;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.SqlServer.Migrations
{
    public partial class Update23 : Migration
    {
        private readonly IElsaDbContextSchema _schema;
        public Update23(IElsaDbContextSchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutputStorageProviderName",
                schema: _schema.Schema,
                table: "WorkflowDefinitions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OutputStorageProviderName",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

using System;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.PostgreSql.Migrations
{
    public partial class Update241 : Migration
    {
        private readonly IElsaDbContextSchema _schema;
        public Update241(IElsaDbContextSchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_DefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "DefinitionVersionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_DefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowInstances");
        }
    }
}

using System;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.SqlServer.Migrations
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
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropIndex(
                name: "IX_Bookmark_CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks");            
            
            migrationBuilder.DropIndex(
                name: "IX_Bookmark_Hash_CorrelationId_TenantId",
                schema: _schema.Schema,
                table: "Bookmarks");
            
            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
            
            migrationBuilder.CreateIndex(
                name: "IX_Bookmark_CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "CorrelationId");
            
            migrationBuilder.CreateIndex(
                name: "IX_Bookmark_Hash_CorrelationId_TenantId",
                schema: _schema.Schema,
                table: "Bookmarks",
                columns: new[] { "Hash", "CorrelationId", "TenantId" });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_Bookmark_CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks");
            
            migrationBuilder.DropIndex(
                name: "IX_Bookmark_Hash_CorrelationId_TenantId",
                schema: _schema.Schema,
                table: "Bookmarks");
            
            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
            
            migrationBuilder.CreateIndex(
                name: "IX_Bookmark_CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "CorrelationId");
            
            migrationBuilder.CreateIndex(
                name: "IX_Bookmark_Hash_CorrelationId_TenantId",
                schema: _schema.Schema,
                table: "Bookmarks",
                columns: new[] { "Hash", "CorrelationId", "TenantId" });
        }
    }
}

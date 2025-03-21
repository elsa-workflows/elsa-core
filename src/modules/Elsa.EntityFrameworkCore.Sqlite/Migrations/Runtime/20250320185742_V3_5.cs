using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_5 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_5(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Triggers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Name",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Name_Hash",
                schema: _schema.Schema,
                table: "Bookmarks",
                columns: new[] { "Name", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Name_Hash_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "Bookmarks",
                columns: new[] { "Name", "Hash", "WorkflowInstanceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_Name",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_Name_Hash",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_Name_Hash_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Triggers",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

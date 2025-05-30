using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Runtime
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
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AggregateFaultCount",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SerializedMetadata",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
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

            migrationBuilder.DropColumn(
                name: "AggregateFaultCount",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "SerializedMetadata",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}

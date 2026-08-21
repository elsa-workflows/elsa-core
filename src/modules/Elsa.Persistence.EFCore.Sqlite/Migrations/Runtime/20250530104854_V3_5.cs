using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_5 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_5(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite does not support schemas, so we omit the schema parameter
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Triggers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Bookmarks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AggregateFaultCount",
                table: "ActivityExecutionRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SerializedMetadata",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Name",
                table: "Bookmarks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Name_Hash",
                table: "Bookmarks",
                columns: new[] { "Name", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Name_Hash_WorkflowInstanceId",
                table: "Bookmarks",
                columns: new[] { "Name", "Hash", "WorkflowInstanceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_Name",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_Name_Hash",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_Name_Hash_WorkflowInstanceId",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "AggregateFaultCount",
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "SerializedMetadata",
                table: "ActivityExecutionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
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

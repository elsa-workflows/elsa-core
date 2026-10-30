using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.MySql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_6 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_6(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // Four varchar(255) utf8mb4 columns are 4,080 bytes, above InnoDB's 3,072-byte limit.
            // 191-character prefixes are 3,056 bytes and uniqueness covers those prefixes, not the full 255.
            // The 64-character name fits MySQL's identifier cap.
            migrationBuilder.Sql($"""
                CREATE UNIQUE INDEX `{StoredTriggerMySqlUniqueIndex.Name}`
                ON `Triggers` (`WorkflowDefinitionId`({StoredTriggerMySqlUniqueIndex.PrefixLength}), `Hash`({StoredTriggerMySqlUniqueIndex.PrefixLength}), `ActivityId`({StoredTriggerMySqlUniqueIndex.PrefixLength}), `TenantId`({StoredTriggerMySqlUniqueIndex.PrefixLength}));
                """);

            // Pomelo SchemaBehavior.Ignore stores tables in DATABASE(), not an Elsa schema.
            migrationBuilder.Sql("""
                SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = DATABASE() AND table_name = 'WorkflowExecutionLogRecords'
                    AND index_name = 'IX_WorkflowExecutionLogRecord_ActivityNodeId');
                SET @sqlstmt := IF(@exist > 0, 'DROP INDEX `IX_WorkflowExecutionLogRecord_ActivityNodeId` ON `WorkflowExecutionLogRecords`', 'SELECT 1');
                PREPARE stmt FROM @sqlstmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = DATABASE() AND table_name = 'ActivityExecutionRecords'
                    AND index_name = 'IX_ActivityExecutionRecord_ActivityNodeId');
                SET @sqlstmt := IF(@exist > 0, 'DROP INDEX `IX_ActivityExecutionRecord_ActivityNodeId` ON `ActivityExecutionRecords`', 'SELECT 1');
                PREPARE stmt FROM @sqlstmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: StoredTriggerMySqlUniqueIndex.Name,
                schema: _schema.Schema,
                table: "Triggers");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "ActivityNodeId");
        }
    }
}

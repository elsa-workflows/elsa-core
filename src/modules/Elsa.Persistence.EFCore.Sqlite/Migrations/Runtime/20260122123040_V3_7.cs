using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_7 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_7(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CallStackDepth",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecords_SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "SchedulingActivityExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecords_SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "SchedulingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecords_SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "SchedulingWorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecords_SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecords_SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecords_SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "CallStackDepth",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");
        }
    }
}

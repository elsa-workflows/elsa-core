using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Runtime
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
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "SchedulingActivityExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "SchedulingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "SchedulingWorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecord_SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecord_SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecord_SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "SchedulingActivityExecutionId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SchedulingActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SchedulingWorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Runtime
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
            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(1024)",
                oldMaxLength: 1024);
        }
    }
}

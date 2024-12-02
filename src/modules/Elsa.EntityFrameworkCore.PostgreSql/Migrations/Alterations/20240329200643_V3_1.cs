using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_1(IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SerializedWorkflowInstanceIds",
                schema: _schema.Schema,
                table: "AlterationPlans",
                newName: "SerializedWorkflowInstanceFilter");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SerializedWorkflowInstanceFilter",
                schema: _schema.Schema,
                table: "AlterationPlans",
                newName: "SerializedWorkflowInstanceIds");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

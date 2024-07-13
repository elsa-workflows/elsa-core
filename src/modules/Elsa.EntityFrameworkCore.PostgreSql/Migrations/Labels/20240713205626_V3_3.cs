using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Labels
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitionLabels",
                newName: "WorkflowDefinitionLabels",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "Labels",
                newName: "Labels",
                newSchema: "Elsa");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LabelId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Labels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: _schema.Schema,
                table: "Labels",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                schema: _schema.Schema,
                table: "Labels",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "Labels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "WorkflowDefinitionLabel_TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels");

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitionLabels",
                schema: _schema.Schema,
                newName: "WorkflowDefinitionLabels");

            migrationBuilder.RenameTable(
                name: "Labels",
                schema: _schema.Schema,
                newName: "Labels");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionVersionId",
                table: "WorkflowDefinitionLabels",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionId",
                table: "WorkflowDefinitionLabels",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LabelId",
                table: "WorkflowDefinitionLabels",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "WorkflowDefinitionLabels",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Labels",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Labels",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Labels",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Labels",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Labels",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

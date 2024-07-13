using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Alterations
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
                name: "AlterationPlans",
                newName: "AlterationPlans",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "AlterationJobs",
                newName: "AlterationJobs",
                newSchema: "Elsa");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedWorkflowInstanceFilter",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedAlterations",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedAt",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedLog",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlanId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedAt",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlterationPlan_TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans");

            migrationBuilder.DropIndex(
                name: "IX_AlterationJob_TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs");

            migrationBuilder.RenameTable(
                name: "AlterationPlans",
                schema: _schema.Schema,
                newName: "AlterationPlans");

            migrationBuilder.RenameTable(
                name: "AlterationJobs",
                schema: _schema.Schema,
                newName: "AlterationJobs");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StartedAt",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedWorkflowInstanceFilter",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedAlterations",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CompletedAt",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AlterationPlans",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StartedAt",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedLog",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlanId",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CompletedAt",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AlterationJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

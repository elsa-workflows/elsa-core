using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: _schema.Schema);

            migrationBuilder.CreateTable(
                name: "AlterationJobs",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    PlanId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    SerializedLog = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlterationJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlterationPlans",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    SerializedAlterations = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedWorkflowInstanceFilter = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlterationPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_CompletedAt",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_CreatedAt",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_PlanId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_StartedAt",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_Status",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_CompletedAt",
                schema: _schema.Schema,
                table: "AlterationPlans",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_CreatedAt",
                schema: _schema.Schema,
                table: "AlterationPlans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_StartedAt",
                schema: _schema.Schema,
                table: "AlterationPlans",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_Status",
                schema: _schema.Schema,
                table: "AlterationPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlterationJobs",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "AlterationPlans",
                schema: _schema.Schema);
        }
    }
}

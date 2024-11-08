using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlterationJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PlanId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedLog = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlterationJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlterationPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedAlterations = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedWorkflowInstanceIds = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlterationPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_CompletedAt",
                table: "AlterationJobs",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_CreatedAt",
                table: "AlterationJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_PlanId",
                table: "AlterationJobs",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_StartedAt",
                table: "AlterationJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_Status",
                table: "AlterationJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_WorkflowInstanceId",
                table: "AlterationJobs",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_CompletedAt",
                table: "AlterationPlans",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_CreatedAt",
                table: "AlterationPlans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_StartedAt",
                table: "AlterationPlans",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_Status",
                table: "AlterationPlans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlterationJobs");

            migrationBuilder.DropTable(
                name: "AlterationPlans");
        }
    }
}

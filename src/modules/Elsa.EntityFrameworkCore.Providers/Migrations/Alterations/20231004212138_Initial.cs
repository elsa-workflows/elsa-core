using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Providers.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "AlterationPlans",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SerializedAlterations = table.Column<string>(type: "text", nullable: true),
                    SerializedWorkflowInstanceIds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlterationPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_CompletedAt",
                schema: "Elsa",
                table: "AlterationPlans",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_CreatedAt",
                schema: "Elsa",
                table: "AlterationPlans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_StartedAt",
                schema: "Elsa",
                table: "AlterationPlans",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_Status",
                schema: "Elsa",
                table: "AlterationPlans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlterationPlans",
                schema: "Elsa");
        }
    }
}

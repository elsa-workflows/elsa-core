using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowSink",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    LastExecutedAt = table.Column<string>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<string>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<string>(type: "TEXT", nullable: true),
                    FaultedAt = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSink", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSink_CancelledAt",
                table: "WorkflowSink",
                column: "CancelledAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSink_CreatedAt",
                table: "WorkflowSink",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSink_FaultedAt",
                table: "WorkflowSink",
                column: "FaultedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSink_FinishedAt",
                table: "WorkflowSink",
                column: "FinishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSink_LastExecutedAt",
                table: "WorkflowSink",
                column: "LastExecutedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowSink");
        }
    }
}

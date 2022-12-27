using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Migrations
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
                name: "IX_WorkflowSink_Id",
                table: "WorkflowSink",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowSink");
        }
    }
}

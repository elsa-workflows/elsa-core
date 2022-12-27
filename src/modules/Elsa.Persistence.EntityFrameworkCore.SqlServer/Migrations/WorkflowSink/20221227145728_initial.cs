using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "WorkflowSink",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastExecutedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FaultedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSink", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSink_Id",
                schema: "Elsa",
                table: "WorkflowSink",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowSink",
                schema: "Elsa");
        }
    }
}

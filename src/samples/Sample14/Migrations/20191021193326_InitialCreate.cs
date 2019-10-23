using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sample14.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionVersions",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    DefinitionId = table.Column<string>(nullable: true),
                    Version = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Activities = table.Column<string>(nullable: true),
                    Connections = table.Column<string>(nullable: true),
                    Variables = table.Column<string>(nullable: true),
                    IsSingleton = table.Column<bool>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    IsPublished = table.Column<bool>(nullable: false),
                    IsLatest = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitionVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    DefinitionId = table.Column<string>(nullable: true),
                    Version = table.Column<int>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    CorrelationId = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    StartedAt = table.Column<DateTime>(nullable: true),
                    FinishedAt = table.Column<DateTime>(nullable: true),
                    FaultedAt = table.Column<DateTime>(nullable: true),
                    AbortedAt = table.Column<DateTime>(nullable: true),
                    Activities = table.Column<string>(nullable: true),
                    Scopes = table.Column<string>(nullable: true),
                    Input = table.Column<string>(nullable: true),
                    BlockingActivities = table.Column<string>(nullable: true),
                    ExecutionLog = table.Column<string>(nullable: true),
                    Fault = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowDefinitionVersions");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");
        }
    }
}

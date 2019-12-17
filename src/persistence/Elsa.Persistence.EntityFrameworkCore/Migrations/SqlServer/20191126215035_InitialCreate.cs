using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFrameworkCore.Migrations.SqlServer
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionVersions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionId = table.Column<string>(nullable: true),
                    DefinitionId = table.Column<string>(nullable: true),
                    Version = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
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
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstanceId = table.Column<string>(nullable: true),
                    DefinitionId = table.Column<string>(nullable: true),
                    Version = table.Column<int>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    CorrelationId = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    StartedAt = table.Column<DateTime>(nullable: true),
                    FinishedAt = table.Column<DateTime>(nullable: true),
                    FaultedAt = table.Column<DateTime>(nullable: true),
                    AbortedAt = table.Column<DateTime>(nullable: true),
                    Scopes = table.Column<string>(nullable: true),
                    Input = table.Column<string>(nullable: true),
                    ExecutionLog = table.Column<string>(nullable: true),
                    Fault = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<string>(nullable: true),
                    WorkflowDefinitionVersionId = table.Column<int>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    Left = table.Column<int>(nullable: false),
                    Top = table.Column<int>(nullable: false),
                    State = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityDefinitions_WorkflowDefinitionVersions_WorkflowDefinitionVersionId",
                        column: x => x.WorkflowDefinitionVersionId,
                        principalTable: "WorkflowDefinitionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowDefinitionVersionId = table.Column<int>(nullable: true),
                    SourceActivityId = table.Column<string>(nullable: true),
                    DestinationActivityId = table.Column<string>(nullable: true),
                    Outcome = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionDefinitions_WorkflowDefinitionVersions_WorkflowDefinitionVersionId",
                        column: x => x.WorkflowDefinitionVersionId,
                        principalTable: "WorkflowDefinitionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityInstances",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<string>(nullable: true),
                    WorkflowInstanceId = table.Column<int>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true),
                    Output = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityInstances_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlockingActivities",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowInstanceId = table.Column<int>(nullable: true),
                    ActivityId = table.Column<string>(nullable: true),
                    ActivityType = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockingActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockingActivities_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinitions_WorkflowDefinitionVersionId",
                table: "ActivityDefinitions",
                column: "WorkflowDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityInstances_WorkflowInstanceId",
                table: "ActivityInstances",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockingActivities_WorkflowInstanceId",
                table: "BlockingActivities",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefinitions_WorkflowDefinitionVersionId",
                table: "ConnectionDefinitions",
                column: "WorkflowDefinitionVersionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDefinitions");

            migrationBuilder.DropTable(
                name: "ActivityInstances");

            migrationBuilder.DropTable(
                name: "BlockingActivities");

            migrationBuilder.DropTable(
                name: "ConnectionDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitionVersions");
        }
    }
}

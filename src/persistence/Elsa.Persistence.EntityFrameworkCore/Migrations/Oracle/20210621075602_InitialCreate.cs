using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFrameworkCore.Migrations.Oracle
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "1, 1"),
                    VersionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DefinitionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Version = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Name = table.Column<string>(type: "NCLOB", nullable: true),
                    Description = table.Column<string>(type: "NCLOB", nullable: true),
                    Variables = table.Column<string>(type: "NCLOB", nullable: true),
                    IsSingleton = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    IsDisabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    IsPublished = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    IsLatest = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitionVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "1, 1"),
                    InstanceId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DefinitionId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Version = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    FaultedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    AbortedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Scope = table.Column<string>(type: "NCLOB", nullable: true),
                    Input = table.Column<string>(type: "NCLOB", nullable: true),
                    ExecutionLog = table.Column<string>(type: "NCLOB", nullable: true),
                    Fault = table.Column<string>(type: "NCLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "1, 1"),
                    ActivityId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    WorkflowDefinitionVersionId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Type = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Name = table.Column<string>(type: "NCLOB", nullable: true),
                    DisplayName = table.Column<string>(type: "NCLOB", nullable: true),
                    Description = table.Column<string>(type: "NCLOB", nullable: true),
                    Left = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Top = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    State = table.Column<string>(type: "NCLOB", nullable: true)
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "1, 1"),
                    WorkflowDefinitionVersionId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    SourceActivityId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DestinationActivityId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Outcome = table.Column<string>(type: "NCLOB", nullable: true)
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "1, 1"),
                    ActivityId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    WorkflowInstanceId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Type = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    State = table.Column<string>(type: "NCLOB", nullable: true),
                    Output = table.Column<string>(type: "NCLOB", nullable: true)
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "1, 1"),
                    WorkflowInstanceId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    ActivityId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ActivityType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
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

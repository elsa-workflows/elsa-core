using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sample23.Migrations.Sqlite
{
    public partial class Create : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "elsa");

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionVersionEntity",
                schema: "elsa",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                    table.PrimaryKey("PK_WorkflowDefinitionVersionEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstanceEntity",
                schema: "elsa",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                    Scope = table.Column<string>(nullable: true),
                    Input = table.Column<string>(nullable: true),
                    ExecutionLog = table.Column<string>(nullable: true),
                    Fault = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstanceEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityDefinitionEntity",
                schema: "elsa",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<string>(nullable: true),
                    WorkflowDefinitionVersionId = table.Column<int>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Left = table.Column<int>(nullable: false),
                    Top = table.Column<int>(nullable: false),
                    State = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDefinitionEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityDefinitionEntity_WorkflowDefinitionVersionEntity_WorkflowDefinitionVersionId",
                        column: x => x.WorkflowDefinitionVersionId,
                        principalSchema: "elsa",
                        principalTable: "WorkflowDefinitionVersionEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionDefinitionEntity",
                schema: "elsa",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowDefinitionVersionId = table.Column<int>(nullable: true),
                    SourceActivityId = table.Column<string>(nullable: true),
                    DestinationActivityId = table.Column<string>(nullable: true),
                    Outcome = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionDefinitionEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionDefinitionEntity_WorkflowDefinitionVersionEntity_WorkflowDefinitionVersionId",
                        column: x => x.WorkflowDefinitionVersionId,
                        principalSchema: "elsa",
                        principalTable: "WorkflowDefinitionVersionEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityInstanceEntity",
                schema: "elsa",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<string>(nullable: true),
                    WorkflowInstanceId = table.Column<int>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true),
                    Output = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityInstanceEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityInstanceEntity_WorkflowInstanceEntity_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "elsa",
                        principalTable: "WorkflowInstanceEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlockingActivityEntity",
                schema: "elsa",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowInstanceId = table.Column<int>(nullable: true),
                    ActivityId = table.Column<string>(nullable: true),
                    ActivityType = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockingActivityEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockingActivityEntity_WorkflowInstanceEntity_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "elsa",
                        principalTable: "WorkflowInstanceEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinitionEntity_WorkflowDefinitionVersionId",
                schema: "elsa",
                table: "ActivityDefinitionEntity",
                column: "WorkflowDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityInstanceEntity_WorkflowInstanceId",
                schema: "elsa",
                table: "ActivityInstanceEntity",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockingActivityEntity_WorkflowInstanceId",
                schema: "elsa",
                table: "BlockingActivityEntity",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefinitionEntity_WorkflowDefinitionVersionId",
                schema: "elsa",
                table: "ConnectionDefinitionEntity",
                column: "WorkflowDefinitionVersionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDefinitionEntity",
                schema: "elsa");

            migrationBuilder.DropTable(
                name: "ActivityInstanceEntity",
                schema: "elsa");

            migrationBuilder.DropTable(
                name: "BlockingActivityEntity",
                schema: "elsa");

            migrationBuilder.DropTable(
                name: "ConnectionDefinitionEntity",
                schema: "elsa");

            migrationBuilder.DropTable(
                name: "WorkflowInstanceEntity",
                schema: "elsa");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitionVersionEntity",
                schema: "elsa");
        }
    }
}

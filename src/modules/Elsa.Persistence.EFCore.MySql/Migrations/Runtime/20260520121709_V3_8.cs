using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.MySql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttempts",
                schema: "Elsa",
                table: "BookmarkQueueItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptedAt",
                schema: "Elsa",
                table: "BookmarkQueueItems",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorMessage",
                schema: "Elsa",
                table: "BookmarkQueueItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LastErrorType",
                schema: "Elsa",
                table: "BookmarkQueueItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BookmarkQueueDeadLetterItems",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalQueueItemId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkflowInstanceId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorrelationId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BookmarkId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StimulusHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActivityInstanceId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActivityTypeName = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalCreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    DeadLetteredAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeliveryAttempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastErrorType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanReplay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReplayedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ReplayedQueueItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerializedOptions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookmarkQueueDeadLetterItems", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_ActivityInstanceId",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_ActivityTypeName",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_BookmarkId",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "BookmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_CorrelationId",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_DeadLetteredAt",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "DeadLetteredAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_OriginalQueueItemId",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "OriginalQueueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_TenantId",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_WorkflowInstanceId",
                schema: "Elsa",
                table: "BookmarkQueueDeadLetterItems",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookmarkQueueDeadLetterItems",
                schema: "Elsa");

            migrationBuilder.DropColumn(
                name: "DeliveryAttempts",
                schema: "Elsa",
                table: "BookmarkQueueItems");

            migrationBuilder.DropColumn(
                name: "LastAttemptedAt",
                schema: "Elsa",
                table: "BookmarkQueueItems");

            migrationBuilder.DropColumn(
                name: "LastErrorMessage",
                schema: "Elsa",
                table: "BookmarkQueueItems");

            migrationBuilder.DropColumn(
                name: "LastErrorType",
                schema: "Elsa",
                table: "BookmarkQueueItems");

        }
    }
}

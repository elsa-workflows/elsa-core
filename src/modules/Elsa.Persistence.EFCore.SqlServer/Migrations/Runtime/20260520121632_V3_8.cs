using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Runtime
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
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorMessage",
                schema: "Elsa",
                table: "BookmarkQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorType",
                schema: "Elsa",
                table: "BookmarkQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookmarkQueueDeadLetterItems",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OriginalQueueItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BookmarkId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StimulusHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivityInstanceId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ActivityTypeName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OriginalCreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeadLetteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryAttempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanReplay = table.Column<bool>(type: "bit", nullable: false),
                    ReplayedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplayedQueueItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerializedOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookmarkQueueDeadLetterItems", x => x.Id);
                });

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

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_8 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_8(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttempts",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptedAt",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorMessage",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorType",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookmarkQueueDeadLetterItems",
                schema: _schema.Schema,
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
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_ActivityTypeName",
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_BookmarkId",
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "BookmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_CorrelationId",
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_DeadLetteredAt",
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "DeadLetteredAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_OriginalQueueItemId",
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "OriginalQueueItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_TenantId",
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueDeadLetterItem_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "BookmarkQueueDeadLetterItems",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookmarkQueueDeadLetterItems",
                schema: _schema.Schema);

            migrationBuilder.DropColumn(
                name: "DeliveryAttempts",
                schema: _schema.Schema,
                table: "BookmarkQueueItems");

            migrationBuilder.DropColumn(
                name: "LastAttemptedAt",
                schema: _schema.Schema,
                table: "BookmarkQueueItems");

            migrationBuilder.DropColumn(
                name: "LastErrorMessage",
                schema: _schema.Schema,
                table: "BookmarkQueueItems");

            migrationBuilder.DropColumn(
                name: "LastErrorType",
                schema: _schema.Schema,
                table: "BookmarkQueueItems");

        }
    }
}

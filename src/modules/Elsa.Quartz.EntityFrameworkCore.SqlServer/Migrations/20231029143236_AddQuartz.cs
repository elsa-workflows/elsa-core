using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Quartz.EntityFrameworkCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddQuartz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "quartz");

            migrationBuilder.CreateTable(
                name: "QRTZ_CALENDARS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CALENDAR_NAME = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CALENDAR = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_CALENDARS", x => new { x.SCHED_NAME, x.CALENDAR_NAME });
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_FIRED_TRIGGERS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ENTRY_ID = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    TRIGGER_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TRIGGER_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    INSTANCE_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FIRED_TIME = table.Column<long>(type: "bigint", nullable: false),
                    SCHED_TIME = table.Column<long>(type: "bigint", nullable: false),
                    PRIORITY = table.Column<int>(type: "int", nullable: false),
                    STATE = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    JOB_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    JOB_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IS_NONCONCURRENT = table.Column<bool>(type: "bit", nullable: false),
                    REQUESTS_RECOVERY = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_FIRED_TRIGGERS", x => new { x.SCHED_NAME, x.ENTRY_ID });
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_JOB_DETAILS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    JOB_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JOB_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    JOB_CLASS_NAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IS_DURABLE = table.Column<bool>(type: "bit", nullable: false),
                    IS_NONCONCURRENT = table.Column<bool>(type: "bit", nullable: false),
                    IS_UPDATE_DATA = table.Column<bool>(type: "bit", nullable: false),
                    REQUESTS_RECOVERY = table.Column<bool>(type: "bit", nullable: false),
                    JOB_DATA = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_JOB_DETAILS", x => new { x.SCHED_NAME, x.JOB_NAME, x.JOB_GROUP });
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_LOCKS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LOCK_NAME = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_LOCKS", x => new { x.SCHED_NAME, x.LOCK_NAME });
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_PAUSED_TRIGGER_GRPS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TRIGGER_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_PAUSED_TRIGGER_GRPS", x => new { x.SCHED_NAME, x.TRIGGER_GROUP });
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_SCHEDULER_STATE",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    INSTANCE_NAME = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LAST_CHECKIN_TIME = table.Column<long>(type: "bigint", nullable: false),
                    CHECKIN_INTERVAL = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_SCHEDULER_STATE", x => new { x.SCHED_NAME, x.INSTANCE_NAME });
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_TRIGGERS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TRIGGER_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TRIGGER_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JOB_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JOB_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NEXT_FIRE_TIME = table.Column<long>(type: "bigint", nullable: true),
                    PREV_FIRE_TIME = table.Column<long>(type: "bigint", nullable: true),
                    PRIORITY = table.Column<int>(type: "int", nullable: true),
                    TRIGGER_STATE = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TRIGGER_TYPE = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    START_TIME = table.Column<long>(type: "bigint", nullable: false),
                    END_TIME = table.Column<long>(type: "bigint", nullable: true),
                    CALENDAR_NAME = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MISFIRE_INSTR = table.Column<short>(type: "smallint", nullable: true),
                    JOB_DATA = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS_SCHED_NAME_JOB_NAME_JOB_GROUP",
                        columns: x => new { x.SCHED_NAME, x.JOB_NAME, x.JOB_GROUP },
                        principalSchema: "quartz",
                        principalTable: "QRTZ_JOB_DETAILS",
                        principalColumns: new[] { "SCHED_NAME", "JOB_NAME", "JOB_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_BLOB_TRIGGERS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TRIGGER_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TRIGGER_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BLOB_DATA = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_BLOB_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_BLOB_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_TRIGGER_GROUP",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalSchema: "quartz",
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_CRON_TRIGGERS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TRIGGER_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TRIGGER_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CRON_EXPRESSION = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TIME_ZONE_ID = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_CRON_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_TRIGGER_GROUP",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalSchema: "quartz",
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_SIMPLE_TRIGGERS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TRIGGER_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TRIGGER_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    REPEAT_COUNT = table.Column<long>(type: "bigint", nullable: false),
                    REPEAT_INTERVAL = table.Column<long>(type: "bigint", nullable: false),
                    TIMES_TRIGGERED = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_SIMPLE_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_TRIGGER_GROUP",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalSchema: "quartz",
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRTZ_SIMPROP_TRIGGERS",
                schema: "quartz",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TRIGGER_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TRIGGER_GROUP = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    STR_PROP_1 = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    STR_PROP_2 = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    STR_PROP_3 = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    INT_PROP_1 = table.Column<int>(type: "int", nullable: true),
                    INT_PROP_2 = table.Column<int>(type: "int", nullable: true),
                    LONG_PROP_1 = table.Column<long>(type: "bigint", nullable: true),
                    LONG_PROP_2 = table.Column<long>(type: "bigint", nullable: true),
                    DEC_PROP_1 = table.Column<decimal>(type: "numeric(13,4)", nullable: true),
                    DEC_PROP_2 = table.Column<decimal>(type: "numeric(13,4)", nullable: true),
                    BOOL_PROP_1 = table.Column<bool>(type: "bit", nullable: true),
                    BOOL_PROP_2 = table.Column<bool>(type: "bit", nullable: true),
                    TIME_ZONE_ID = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_SIMPROP_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_TRIGGER_GROUP",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalSchema: "quartz",
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_JOB_GROUP",
                schema: "quartz",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "JOB_GROUP");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_JOB_NAME",
                schema: "quartz",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "JOB_NAME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_JOB_REQ_RECOVERY",
                schema: "quartz",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "REQUESTS_RECOVERY");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_GROUP",
                schema: "quartz",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "TRIGGER_GROUP");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_INST_NAME",
                schema: "quartz",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "INSTANCE_NAME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_NAME",
                schema: "quartz",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "TRIGGER_NAME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_NM_GP",
                schema: "quartz",
                table: "QRTZ_FIRED_TRIGGERS",
                columns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" });

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_J_REQ_RECOVERY",
                schema: "quartz",
                table: "QRTZ_JOB_DETAILS",
                column: "REQUESTS_RECOVERY");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_T_NEXT_FIRE_TIME",
                schema: "quartz",
                table: "QRTZ_TRIGGERS",
                column: "NEXT_FIRE_TIME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_T_NFT_ST",
                schema: "quartz",
                table: "QRTZ_TRIGGERS",
                columns: new[] { "NEXT_FIRE_TIME", "TRIGGER_STATE" });

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_T_STATE",
                schema: "quartz",
                table: "QRTZ_TRIGGERS",
                column: "TRIGGER_STATE");

            migrationBuilder.CreateIndex(
                name: "IX_QRTZ_TRIGGERS_SCHED_NAME_JOB_NAME_JOB_GROUP",
                schema: "quartz",
                table: "QRTZ_TRIGGERS",
                columns: new[] { "SCHED_NAME", "JOB_NAME", "JOB_GROUP" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QRTZ_BLOB_TRIGGERS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_CALENDARS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_CRON_TRIGGERS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_FIRED_TRIGGERS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_LOCKS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_PAUSED_TRIGGER_GRPS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_SCHEDULER_STATE",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_SIMPLE_TRIGGERS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_SIMPROP_TRIGGERS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_TRIGGERS",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "QRTZ_JOB_DETAILS",
                schema: "quartz");
        }
    }
}

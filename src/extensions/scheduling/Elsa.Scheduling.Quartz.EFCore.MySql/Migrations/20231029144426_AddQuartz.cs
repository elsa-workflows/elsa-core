using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Scheduling.Quartz.EFCore.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddQuartz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_CALENDARS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CALENDAR_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CALENDAR = table.Column<byte[]>(type: "blob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_CALENDARS", x => new { x.SCHED_NAME, x.CALENDAR_NAME });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_FIRED_TRIGGERS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ENTRY_ID = table.Column<string>(type: "varchar(140)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    INSTANCE_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FIRED_TIME = table.Column<long>(type: "bigint(19)", nullable: false),
                    SCHED_TIME = table.Column<long>(type: "bigint(19)", nullable: false),
                    PRIORITY = table.Column<int>(type: "integer", nullable: false),
                    STATE = table.Column<string>(type: "varchar(16)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JOB_NAME = table.Column<string>(type: "varchar(200)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JOB_GROUP = table.Column<string>(type: "varchar(200)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IS_NONCONCURRENT = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    REQUESTS_RECOVERY = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_FIRED_TRIGGERS", x => new { x.SCHED_NAME, x.ENTRY_ID });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_JOB_DETAILS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JOB_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JOB_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DESCRIPTION = table.Column<string>(type: "varchar(250)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JOB_CLASS_NAME = table.Column<string>(type: "varchar(250)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IS_DURABLE = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IS_NONCONCURRENT = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IS_UPDATE_DATA = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    REQUESTS_RECOVERY = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    JOB_DATA = table.Column<byte[]>(type: "blob", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_JOB_DETAILS", x => new { x.SCHED_NAME, x.JOB_NAME, x.JOB_GROUP });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_LOCKS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LOCK_NAME = table.Column<string>(type: "varchar(40)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_LOCKS", x => new { x.SCHED_NAME, x.LOCK_NAME });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_PAUSED_TRIGGER_GRPS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_PAUSED_TRIGGER_GRPS", x => new { x.SCHED_NAME, x.TRIGGER_GROUP });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_SCHEDULER_STATE",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    INSTANCE_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LAST_CHECKIN_TIME = table.Column<long>(type: "bigint(19)", nullable: false),
                    CHECKIN_INTERVAL = table.Column<long>(type: "bigint(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_SCHEDULER_STATE", x => new { x.SCHED_NAME, x.INSTANCE_NAME });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_TRIGGERS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JOB_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JOB_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DESCRIPTION = table.Column<string>(type: "varchar(250)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NEXT_FIRE_TIME = table.Column<long>(type: "bigint(19)", nullable: true),
                    PREV_FIRE_TIME = table.Column<long>(type: "bigint(19)", nullable: true),
                    PRIORITY = table.Column<int>(type: "integer", nullable: true),
                    TRIGGER_STATE = table.Column<string>(type: "varchar(16)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_TYPE = table.Column<string>(type: "varchar(8)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    START_TIME = table.Column<long>(type: "bigint(19)", nullable: false),
                    END_TIME = table.Column<long>(type: "bigint(19)", nullable: true),
                    CALENDAR_NAME = table.Column<string>(type: "varchar(200)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MISFIRE_INSTR = table.Column<short>(type: "smallint(2)", nullable: true),
                    JOB_DATA = table.Column<byte[]>(type: "blob", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS_SCHED_NAME_JOB_NAME_JOB_GROUP",
                        columns: x => new { x.SCHED_NAME, x.JOB_NAME, x.JOB_GROUP },
                        principalTable: "QRTZ_JOB_DETAILS",
                        principalColumns: new[] { "SCHED_NAME", "JOB_NAME", "JOB_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_BLOB_TRIGGERS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BLOB_DATA = table.Column<byte[]>(type: "blob", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_BLOB_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_BLOB_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_TRI~",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_CRON_TRIGGERS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CRON_EXPRESSION = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TIME_ZONE_ID = table.Column<string>(type: "varchar(80)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_CRON_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_TRI~",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_SIMPLE_TRIGGERS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    REPEAT_COUNT = table.Column<long>(type: "bigint(7)", nullable: false),
                    REPEAT_INTERVAL = table.Column<long>(type: "bigint(12)", nullable: false),
                    TIMES_TRIGGERED = table.Column<long>(type: "bigint(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_SIMPLE_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_T~",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QRTZ_SIMPROP_TRIGGERS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "varchar(120)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_NAME = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TRIGGER_GROUP = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    STR_PROP_1 = table.Column<string>(type: "varchar(512)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    STR_PROP_2 = table.Column<string>(type: "varchar(512)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    STR_PROP_3 = table.Column<string>(type: "varchar(512)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    INT_PROP_1 = table.Column<int>(type: "int", nullable: true),
                    INT_PROP_2 = table.Column<int>(type: "int", nullable: true),
                    LONG_PROP_1 = table.Column<long>(type: "BIGINT", nullable: true),
                    LONG_PROP_2 = table.Column<long>(type: "BIGINT", nullable: true),
                    DEC_PROP_1 = table.Column<decimal>(type: "NUMERIC(13,4)", nullable: true),
                    DEC_PROP_2 = table.Column<decimal>(type: "NUMERIC(13,4)", nullable: true),
                    BOOL_PROP_1 = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    BOOL_PROP_2 = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    TIME_ZONE_ID = table.Column<string>(type: "varchar(80)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRTZ_SIMPROP_TRIGGERS", x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP });
                    table.ForeignKey(
                        name: "FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS_SCHED_NAME_TRIGGER_NAME_~",
                        columns: x => new { x.SCHED_NAME, x.TRIGGER_NAME, x.TRIGGER_GROUP },
                        principalTable: "QRTZ_TRIGGERS",
                        principalColumns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_JOB_GROUP",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "JOB_GROUP");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_JOB_NAME",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "JOB_NAME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_JOB_REQ_RECOVERY",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "REQUESTS_RECOVERY");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_GROUP",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "TRIGGER_GROUP");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_INST_NAME",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "INSTANCE_NAME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_NAME",
                table: "QRTZ_FIRED_TRIGGERS",
                column: "TRIGGER_NAME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_FT_TRIG_NM_GP",
                table: "QRTZ_FIRED_TRIGGERS",
                columns: new[] { "SCHED_NAME", "TRIGGER_NAME", "TRIGGER_GROUP" });

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_J_REQ_RECOVERY",
                table: "QRTZ_JOB_DETAILS",
                column: "REQUESTS_RECOVERY");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_T_NEXT_FIRE_TIME",
                table: "QRTZ_TRIGGERS",
                column: "NEXT_FIRE_TIME");

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_T_NFT_ST",
                table: "QRTZ_TRIGGERS",
                columns: new[] { "NEXT_FIRE_TIME", "TRIGGER_STATE" });

            migrationBuilder.CreateIndex(
                name: "IDX_QRTZ_T_STATE",
                table: "QRTZ_TRIGGERS",
                column: "TRIGGER_STATE");

            migrationBuilder.CreateIndex(
                name: "IX_QRTZ_TRIGGERS_SCHED_NAME_JOB_NAME_JOB_GROUP",
                table: "QRTZ_TRIGGERS",
                columns: new[] { "SCHED_NAME", "JOB_NAME", "JOB_GROUP" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QRTZ_BLOB_TRIGGERS");

            migrationBuilder.DropTable(
                name: "QRTZ_CALENDARS");

            migrationBuilder.DropTable(
                name: "QRTZ_CRON_TRIGGERS");

            migrationBuilder.DropTable(
                name: "QRTZ_FIRED_TRIGGERS");

            migrationBuilder.DropTable(
                name: "QRTZ_LOCKS");

            migrationBuilder.DropTable(
                name: "QRTZ_PAUSED_TRIGGER_GRPS");

            migrationBuilder.DropTable(
                name: "QRTZ_SCHEDULER_STATE");

            migrationBuilder.DropTable(
                name: "QRTZ_SIMPLE_TRIGGERS");

            migrationBuilder.DropTable(
                name: "QRTZ_SIMPROP_TRIGGERS");

            migrationBuilder.DropTable(
                name: "QRTZ_TRIGGERS");

            migrationBuilder.DropTable(
                name: "QRTZ_JOB_DETAILS");
        }
    }
}

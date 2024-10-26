using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Labels
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionLabels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "TEXT", nullable: false),
                    LabelId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitionLabels", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_LabelId",
                table: "WorkflowDefinitionLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_TenantId",
                table: "WorkflowDefinitionLabels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_WorkflowDefinitionId",
                table: "WorkflowDefinitionLabels",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_WorkflowDefinitionVersionId",
                table: "WorkflowDefinitionLabels",
                column: "WorkflowDefinitionVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitionLabels");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Oracle.Migrations.Labels
{
    /// <inheritdoc />
    public partial class V3_2 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_2(Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "Labels",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    NormalizedName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Color = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionLabels",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    LabelId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitionLabels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_LabelId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_WorkflowDefinitionId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_WorkflowDefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                column: "WorkflowDefinitionVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Labels",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "WorkflowDefinitionLabels",
                schema: _schema.Schema);
        }
    }
}

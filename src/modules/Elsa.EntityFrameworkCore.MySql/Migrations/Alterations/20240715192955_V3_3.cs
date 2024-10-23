#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.EntityFrameworkCore.MySql.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                    name: "TenantId",
                    schema: _schema.Schema,
                    table: "AlterationPlans",
                    type: "varchar(255)",
                    nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                    name: "TenantId",
                    schema: _schema.Schema,
                    table: "AlterationJobs",
                    type: "varchar(255)",
                    nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationPlan_TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlterationJob_TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlterationPlan_TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans");

            migrationBuilder.DropIndex(
                name: "IX_AlterationJob_TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "AlterationPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "AlterationJobs");
        }
    }
}
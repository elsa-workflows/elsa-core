using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Connections.Persistence.EntityFrameworkCore.Sqlite.Migrations
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
            migrationBuilder.EnsureSchema(
                name: _schema.Schema);

            migrationBuilder.CreateTable(
                name: "ConnectionDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionConfiguration = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionType = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefinition_Name",
                schema: _schema.Schema,
                table: "ConnectionDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefinition_TenantId",
                schema: _schema.Schema,
                table: "ConnectionDefinitions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionDefinitions",
                schema: _schema.Schema);
        }
    }
}

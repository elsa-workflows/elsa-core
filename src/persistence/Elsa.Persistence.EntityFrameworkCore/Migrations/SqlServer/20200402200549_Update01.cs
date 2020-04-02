using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFrameworkCore.Migrations.SqlServer
{
    public partial class Update01 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ActivityDefinitions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ActivityDefinitions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ActivityDefinitions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ActivityDefinitions");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ActivityDefinitions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ActivityDefinitions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Secrets.Persistence.EntityFramework.MySql.Migrations
{
    public partial class RequireName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Elsa",
                table: "Secrets",
                type: "varchar(255)",
                nullable: false,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Elsa",
                table: "Secrets",
                type: "varchar(255)",
                nullable: true);
        }
    }
}

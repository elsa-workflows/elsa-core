using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Identity
{
    /// <inheritdoc />
    public partial class V3_2 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_2(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

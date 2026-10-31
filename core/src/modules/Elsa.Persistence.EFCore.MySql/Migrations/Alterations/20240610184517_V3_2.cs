#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore.MySql.Migrations.Alterations
{
    /// <inheritdoc />
    public partial class V3_2 : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_2(IElsaDbContextSchema schema)
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

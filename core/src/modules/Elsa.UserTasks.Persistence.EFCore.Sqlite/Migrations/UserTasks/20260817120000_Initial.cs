using Elsa.Persistence.EFCore;
using Elsa.UserTasks.Persistence.EFCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.UserTasks.Persistence.EFCore.Sqlite.Migrations.UserTasks;

[DbContext(typeof(UserTasksElsaDbContext))]
[Migration("20260817120000_Initial")]
public partial class Initial : Migration
{
    private readonly IElsaDbContextSchema _schema;

    public Initial(IElsaDbContextSchema schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    protected override void Up(MigrationBuilder migrationBuilder) => UserTasksSchemaMigration.Up(migrationBuilder, _schema.Schema);

    protected override void Down(MigrationBuilder migrationBuilder) => UserTasksSchemaMigration.Down(migrationBuilder, _schema.Schema);
}

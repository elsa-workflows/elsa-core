using Elsa.Persistence.EFCore;
using Elsa.UserTasks.Persistence.EFCore;
using Elsa.UserTasks.Persistence.EFCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.UserTasks.Persistence.EFCore.PostgreSql.Migrations.UserTasks;

[DbContext(typeof(UserTasksElsaDbContext))]
[Migration("20260817120000_Initial")]
public partial class Initial : Migration
{
    private readonly IElsaDbContextSchema schema;
    public Initial(IElsaDbContextSchema schema) => this.schema = schema ?? throw new ArgumentNullException(nameof(schema));
    protected override void Up(MigrationBuilder migrationBuilder) => UserTasksSchemaMigration.Up(migrationBuilder, schema.Schema);
    protected override void Down(MigrationBuilder migrationBuilder) => UserTasksSchemaMigration.Down(migrationBuilder, schema.Schema);
}

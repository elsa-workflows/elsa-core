using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Persistence.EFCore.Sqlite;

[UsedImplicitly]
public class UserTasksDbContextFactory : SqliteUserTasksDesignTimeDbContextFactory<UserTasksElsaDbContext>;

public class SqliteUserTasksDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSqliteEntityModelCreatingHandlers();
    }

    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}

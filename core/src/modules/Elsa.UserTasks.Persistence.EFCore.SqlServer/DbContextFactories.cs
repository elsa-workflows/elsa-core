using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.UserTasks.Persistence.EFCore.SqlServer;

[UsedImplicitly]
public class UserTasksDbContextFactory : SqlServerUserTasksDesignTimeDbContextFactory<UserTasksElsaDbContext>;

public class SqlServerUserTasksDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString) => builder.UseElsaSqlServer(GetType().Assembly, connectionString);
}

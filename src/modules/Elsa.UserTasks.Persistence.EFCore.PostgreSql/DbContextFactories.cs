using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Persistence.EFCore.PostgreSql;

[UsedImplicitly]
public class UserTasksDbContextFactory : PostgreSqlUserTasksDesignTimeDbContextFactory<UserTasksElsaDbContext>;
public class PostgreSqlUserTasksDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureServices(IServiceCollection services) => services.AddPostgreSqlEntityModelCreatingHandlers();
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString) => builder.UseElsaPostgreSql(GetType().Assembly, connectionString);
}

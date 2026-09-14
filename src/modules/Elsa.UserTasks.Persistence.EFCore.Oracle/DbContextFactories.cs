using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.UserTasks.Persistence.EFCore.Oracle;

[UsedImplicitly]
public class UserTasksDbContextFactory : OracleUserTasksDesignTimeDbContextFactory<UserTasksElsaDbContext>;
public class OracleUserTasksDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString) => builder.UseElsaOracle(GetType().Assembly, connectionString, new ElsaDbContextOptions().ConfigureOracle());
}

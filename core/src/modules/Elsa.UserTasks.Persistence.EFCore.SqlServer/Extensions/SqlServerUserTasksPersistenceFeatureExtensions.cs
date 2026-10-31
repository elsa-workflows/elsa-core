using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.UserTasks.Persistence.EFCore.SqlServer.Extensions;

public static class SqlServerUserTasksPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqlServerUserTasksPersistenceFeatureExtensions).Assembly;

    public static EFCoreUserTasksPersistenceFeature UseSqlServer(this EFCoreUserTasksPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null, Action<SqlServerDbContextOptionsBuilder>? configure = null) => feature.UseSqlServer(Assembly, connectionString, options, configure);

    public static EFCoreUserTasksPersistenceFeature UseSqlServer(this EFCoreUserTasksPersistenceFeature feature, Func<IServiceProvider, string> connectionStringFunc, ElsaDbContextOptions? options = null, Action<SqlServerDbContextOptionsBuilder>? configure = null) => feature.UseSqlServer(Assembly, connectionStringFunc, options, configure);
}

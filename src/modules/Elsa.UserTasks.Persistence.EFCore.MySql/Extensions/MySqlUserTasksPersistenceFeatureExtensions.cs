using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.UserTasks.Persistence.EFCore.MySql.Extensions;

public static class MySqlUserTasksPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(MySqlUserTasksPersistenceFeatureExtensions).Assembly;
    public static EFCoreUserTasksPersistenceFeature UseMySql(this EFCoreUserTasksPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null, Action<MySqlDbContextOptionsBuilder>? configure = null) => feature.UseMySql(Assembly, connectionString, options, configure);
    public static EFCoreUserTasksPersistenceFeature UseMySql(this EFCoreUserTasksPersistenceFeature feature, Func<IServiceProvider, string> connectionStringFunc, ElsaDbContextOptions? options = null, Action<MySqlDbContextOptionsBuilder>? configure = null) => feature.UseMySql(Assembly, connectionStringFunc, options, configure);
}

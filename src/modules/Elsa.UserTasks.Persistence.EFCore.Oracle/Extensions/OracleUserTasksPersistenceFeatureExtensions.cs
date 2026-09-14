using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore.Features;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.UserTasks.Persistence.EFCore.Oracle.Extensions;

public static class OracleUserTasksPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(OracleUserTasksPersistenceFeatureExtensions).Assembly;
    public static EFCoreUserTasksPersistenceFeature UseOracle(this EFCoreUserTasksPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null, Action<OracleDbContextOptionsBuilder>? configure = null) => feature.UseOracle(Assembly, connectionString, options, configure);
    public static EFCoreUserTasksPersistenceFeature UseOracle(this EFCoreUserTasksPersistenceFeature feature, Func<IServiceProvider, string> connectionStringFunc, ElsaDbContextOptions? options = null, Action<OracleDbContextOptionsBuilder>? configure = null) => feature.UseOracle(Assembly, connectionStringFunc, options, configure);
}

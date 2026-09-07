using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore.Features;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.UserTasks.Persistence.EFCore.PostgreSql.Extensions;

public static class PostgreSqlUserTasksPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlUserTasksPersistenceFeatureExtensions).Assembly;
    public static EFCoreUserTasksPersistenceFeature UsePostgreSql(this EFCoreUserTasksPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null, Action<NpgsqlDbContextOptionsBuilder>? configure = null) => feature.UsePostgreSql(Assembly, connectionString, options, configure);
    public static EFCoreUserTasksPersistenceFeature UsePostgreSql(this EFCoreUserTasksPersistenceFeature feature, Func<IServiceProvider, string> connectionStringFunc, ElsaDbContextOptions? options = null, Action<NpgsqlDbContextOptionsBuilder>? configure = null) => feature.UsePostgreSql(Assembly, connectionStringFunc, options, configure);
}

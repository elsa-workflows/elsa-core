using Elsa.ExternalAuthentication.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.PostgreSql.Extensions;

public static class PostgreSqlExternalAuthenticationPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlExternalAuthenticationPersistenceFeatureExtensions).Assembly;

    public static EFCoreExternalAuthenticationPersistenceFeature UsePostgreSql(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UsePostgreSql(_ => connectionString, options, configure);
    }

    public static EFCoreExternalAuthenticationPersistenceFeature UsePostgreSql(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UsePostgreSql(Assembly, connectionStringFunc, options, configure);
    }
}

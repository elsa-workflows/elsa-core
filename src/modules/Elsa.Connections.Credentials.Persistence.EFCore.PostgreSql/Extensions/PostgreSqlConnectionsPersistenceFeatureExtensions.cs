using System.Reflection;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Extensions;

public static class PostgreSqlConnectionsPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlConnectionsPersistenceFeatureExtensions).Assembly;

    public static EFCoreConnectionsPersistenceFeature UsePostgreSql(
        this EFCoreConnectionsPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UsePostgreSql(_ => connectionString, options, configure);
    }

    public static EFCoreConnectionsPersistenceFeature UsePostgreSql(
        this EFCoreConnectionsPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        feature.Services.TryAddSingleton<IConnectionCredentialBindingConflictClassifier, PostgreSqlConnectionCredentialBindingConflictClassifier>();
        return feature.UsePostgreSql(Assembly, connectionStringFunc, options, configure);
    }
}

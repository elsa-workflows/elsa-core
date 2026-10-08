using System.Reflection;
using Elsa.AI.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.AI.Persistence.EFCore.PostgreSql.Extensions;

public static class PostgreSqlAIPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlAIPersistenceFeatureExtensions).Assembly;

    public static AIPersistenceFeature UsePostgreSql(
        this AIPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UsePostgreSql(_ => connectionString, options, configure);
    }

    public static AIPersistenceFeature UsePostgreSql(
        this AIPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UsePostgreSql(Assembly, connectionStringFunc, options, configure);
    }
}

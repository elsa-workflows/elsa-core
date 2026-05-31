using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Features;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Secrets.Persistence.EFCore.PostgreSql.Extensions;

public static class PostgreSqlSecretsPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlSecretsPersistenceFeatureExtensions).Assembly;

    public static EFCoreSecretsPersistenceFeature UsePostgreSql(
        this EFCoreSecretsPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UsePostgreSql(_ => connectionString, options, configure);
    }

    public static EFCoreSecretsPersistenceFeature UsePostgreSql(
        this EFCoreSecretsPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        feature.Module.Services.AddPostgreSqlEntityModelCreatingHandlers();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaPostgreSql(Assembly, connectionStringFunc(sp), options, configure);
        return feature;
    }
}

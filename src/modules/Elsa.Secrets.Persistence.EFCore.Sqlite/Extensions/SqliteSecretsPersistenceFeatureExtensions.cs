using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;

public static class SqliteSecretsPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqliteSecretsPersistenceFeatureExtensions).Assembly;

    public static EFCoreSecretsPersistenceFeature UseSqlite(
        this EFCoreSecretsPersistenceFeature feature,
        string? connectionString = null,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        return feature.UseSqlite(_ => connectionString, options, configure);
    }

    public static EFCoreSecretsPersistenceFeature UseSqlite(
        this EFCoreSecretsPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        feature.Module.Services.AddSqliteEntityModelCreatingHandlers();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaSqlite(Assembly, connectionStringFunc(sp), options, configure);
        return feature;
    }
}

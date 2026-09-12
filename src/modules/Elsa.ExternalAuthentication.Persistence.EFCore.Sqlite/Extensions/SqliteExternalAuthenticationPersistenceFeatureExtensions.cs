using Elsa.ExternalAuthentication.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.Extensions;

public static class SqliteExternalAuthenticationPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqliteExternalAuthenticationPersistenceFeatureExtensions).Assembly;

    public static EFCoreExternalAuthenticationPersistenceFeature UseSqlite(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        string? connectionString = null,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        return feature.UseSqlite(_ => connectionString, options, configure);
    }

    public static EFCoreExternalAuthenticationPersistenceFeature UseSqlite(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlite(Assembly, connectionStringFunc, options, configure);
    }
}

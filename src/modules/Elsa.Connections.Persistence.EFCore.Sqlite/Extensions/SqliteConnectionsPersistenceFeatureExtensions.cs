using Elsa.Connections.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace Elsa.Connections.Persistence.EFCore.Sqlite.Extensions;

public static class SqliteConnectionsPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqliteConnectionsPersistenceFeatureExtensions).Assembly;

    public static EFCoreConnectionsPersistenceFeature UseSqlite(
        this EFCoreConnectionsPersistenceFeature feature,
        string? connectionString = null,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        return feature.UseSqlite(_ => connectionString, options, configure);
    }

    public static EFCoreConnectionsPersistenceFeature UseSqlite(
        this EFCoreConnectionsPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null) =>
        feature.UseSqlite(Assembly, connectionStringFunc, options, configure);
}

using System.Reflection;
using Elsa.AI.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.AI.Persistence.EFCore.Sqlite.Extensions;

public static class SqliteAIPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqliteAIPersistenceFeatureExtensions).Assembly;

    public static AIPersistenceFeature UseSqlite(
        this AIPersistenceFeature feature,
        string? connectionString = null,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        return feature.UseSqlite(_ => connectionString, options, configure);
    }

    public static AIPersistenceFeature UseSqlite(
        this AIPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlite(Assembly, connectionStringFunc, options, configure);
    }
}

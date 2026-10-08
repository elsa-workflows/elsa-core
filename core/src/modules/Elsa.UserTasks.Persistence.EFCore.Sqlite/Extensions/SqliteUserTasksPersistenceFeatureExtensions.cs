using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.EFCore.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.UserTasks.Persistence.EFCore.Sqlite.Extensions;

public static class SqliteUserTasksPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqliteUserTasksPersistenceFeatureExtensions).Assembly;

    public static EFCoreUserTasksPersistenceFeature UseSqlite(
        this EFCoreUserTasksPersistenceFeature feature,
        string? connectionString = null,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        return feature.UseSqlite(_ => connectionString, options, configure);
    }

    public static EFCoreUserTasksPersistenceFeature UseSqlite(
        this EFCoreUserTasksPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlite(Assembly, connectionStringFunc, options, configure);
    }
}

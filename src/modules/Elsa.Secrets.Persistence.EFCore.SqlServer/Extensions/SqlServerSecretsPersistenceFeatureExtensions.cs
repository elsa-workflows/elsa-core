using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Secrets.Persistence.EFCore.SqlServer.Extensions;

public static class SqlServerSecretsPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqlServerSecretsPersistenceFeatureExtensions).Assembly;

    public static EFCoreSecretsPersistenceFeature UseSqlServer(
        this EFCoreSecretsPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlServer(_ => connectionString, options, configure);
    }

    public static EFCoreSecretsPersistenceFeature UseSqlServer(
        this EFCoreSecretsPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
    {
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaSqlServer(Assembly, connectionStringFunc(sp), options, configure);
        return feature;
    }
}

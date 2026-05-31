using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Secrets.Persistence.EFCore.MySql.Extensions;

public static class MySqlSecretsPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(MySqlSecretsPersistenceFeatureExtensions).Assembly;

    public static EFCoreSecretsPersistenceFeature UseMySql(
        this EFCoreSecretsPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<MySqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseMySql(_ => connectionString, options, configure);
    }

    public static EFCoreSecretsPersistenceFeature UseMySql(
        this EFCoreSecretsPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<MySqlDbContextOptionsBuilder>? configure = null)
    {
        feature.Module.Services.AddMySqlEntityModelCreatingHandlers();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaMySql(Assembly, connectionStringFunc(sp), options, configure: configure);
        return feature;
    }
}

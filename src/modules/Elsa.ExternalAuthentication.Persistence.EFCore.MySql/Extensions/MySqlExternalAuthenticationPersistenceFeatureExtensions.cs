using Elsa.ExternalAuthentication.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.MySql.Extensions;

public static class MySqlExternalAuthenticationPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(MySqlExternalAuthenticationPersistenceFeatureExtensions).Assembly;

    public static EFCoreExternalAuthenticationPersistenceFeature UseMySql(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<MySqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseMySql(_ => connectionString, options, configure);
    }

    public static EFCoreExternalAuthenticationPersistenceFeature UseMySql(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<MySqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseMySql(Assembly, connectionStringFunc, options, configure);
    }
}

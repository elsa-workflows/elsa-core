using Elsa.ExternalAuthentication.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.SqlServer.Extensions;

public static class SqlServerExternalAuthenticationPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqlServerExternalAuthenticationPersistenceFeatureExtensions).Assembly;

    public static EFCoreExternalAuthenticationPersistenceFeature UseSqlServer(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlServer(_ => connectionString, options, configure);
    }

    public static EFCoreExternalAuthenticationPersistenceFeature UseSqlServer(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlServer(Assembly, connectionStringFunc, options, configure);
    }
}

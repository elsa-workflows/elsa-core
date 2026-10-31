using System.Reflection;
using Elsa.AI.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.AI.Persistence.EFCore.SqlServer.Extensions;

public static class SqlServerAIPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(SqlServerAIPersistenceFeatureExtensions).Assembly;

    public static AIPersistenceFeature UseSqlServer(
        this AIPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlServer(_ => connectionString, options, configure);
    }

    public static AIPersistenceFeature UseSqlServer(
        this AIPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseSqlServer(Assembly, connectionStringFunc, options, configure);
    }
}

using System.Reflection;
using Elsa.AI.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.AI.Persistence.EFCore.MySql.Extensions;

public static class MySqlAIPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(MySqlAIPersistenceFeatureExtensions).Assembly;

    public static AIPersistenceFeature UseMySql(
        this AIPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<MySqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseMySql(_ => connectionString, options, configure);
    }

    public static AIPersistenceFeature UseMySql(
        this AIPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<MySqlDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseMySql(Assembly, connectionStringFunc, options, configure);
    }
}

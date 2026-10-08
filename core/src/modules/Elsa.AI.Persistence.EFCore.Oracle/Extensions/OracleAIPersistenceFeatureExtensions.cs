using System.Reflection;
using Elsa.AI.Persistence.EFCore.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.AI.Persistence.EFCore.Oracle.Extensions;

public static class OracleAIPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(OracleAIPersistenceFeatureExtensions).Assembly;

    public static AIPersistenceFeature UseOracle(
        this AIPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseOracle(_ => connectionString, options, configure);
    }

    public static AIPersistenceFeature UseOracle(
        this AIPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseOracle(Assembly, connectionStringFunc, options, configure);
    }
}

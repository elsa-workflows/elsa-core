using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowRuntimePersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UseSqlServer(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString, options);
        return feature;
    }
}
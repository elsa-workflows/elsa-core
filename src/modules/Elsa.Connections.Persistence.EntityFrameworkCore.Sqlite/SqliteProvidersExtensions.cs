using System.Reflection;
using Elsa.Connections.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Sqlite.
/// </summary>
public static class ConnectionsSqliteProvidersExtensions
{
    private static Assembly Assembly => typeof(ConnectionsSqliteProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static EFCoreConnectionPersistenceFeature UseSqlite(this EFCoreConnectionPersistenceFeature feature, string? connectionString = null, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlite(Assembly, connectionString, options);
        return feature;
    }
    
    public static EFCoreConnectionPersistenceFeature UseSqlite(this EFCoreConnectionPersistenceFeature feature, Func<IServiceProvider, string> connectionStringFunc, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlite(Assembly, connectionStringFunc, options);
        return feature;
    }
}
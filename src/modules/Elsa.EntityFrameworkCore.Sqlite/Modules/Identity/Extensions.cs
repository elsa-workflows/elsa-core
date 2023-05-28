using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Identity;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> feature with SQLite persistence providers.
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> feature with SQLite persistence providers.
    /// </summary>
    /// <param name="feature">The feature to configure.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <param name="options">Options specified via <see cref="ElsaDbContextOptions"/> allows to configure for manual database migrations.</param>
    /// <returns>The configured feature.</returns>
    public static EFCoreIdentityPersistenceFeature UseSqlite(this EFCoreIdentityPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
    }
}
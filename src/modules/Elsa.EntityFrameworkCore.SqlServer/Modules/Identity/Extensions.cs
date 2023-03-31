using Elsa.EntityFrameworkCore.Modules.Identity;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> feature with SQL Server persistence providers.
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> feature with SQL Server persistence providers.
    /// </summary>
    /// <param name="feature">The feature to configure.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>The configured feature.</returns>
    public static EFCoreIdentityPersistenceFeature UseSqlServer(this EFCoreIdentityPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}
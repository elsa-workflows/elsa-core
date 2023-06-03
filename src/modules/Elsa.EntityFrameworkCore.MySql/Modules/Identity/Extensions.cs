using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Common;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> feature with MySql persistence providers.
    /// </summary>
    /// <param name="feature">The feature to configure.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <param name="options">Options specified via <see cref="ElsaDbContextOptions"/> allows to configure for manual database migrations.</param>
    /// <returns>The configured feature.</returns>
    public static EFCoreIdentityPersistenceFeature UseMySql(this EFCoreIdentityPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString, options);
        return feature;
    }
}
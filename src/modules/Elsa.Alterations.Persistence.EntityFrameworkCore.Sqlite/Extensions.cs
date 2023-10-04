using Elsa.Alterations.Persistence.EntityFrameworkCore;
using Elsa.EntityFrameworkCore.Common;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Contains extension methods for <see cref="EFCoreAlterationPersistenceFeature"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static EFCoreAlterationPersistenceFeature UseSqlite(this EFCoreAlterationPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
    }
}
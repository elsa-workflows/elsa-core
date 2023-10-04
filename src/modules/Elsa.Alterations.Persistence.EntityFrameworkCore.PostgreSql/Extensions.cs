using Elsa.Alterations.Persistence.EntityFrameworkCore;
using Elsa.EntityFrameworkCore.Common;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Contains extension methods for <see cref="EFCoreAlterationPersistenceFeature"/>.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreAlterationPersistenceFeature UseSqlServer(this EFCoreAlterationPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString, options);
        return feature;
    }
}
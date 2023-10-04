using Elsa.Alterations.Persistence.EntityFrameworkCore;
using Elsa.EntityFrameworkCore.Common;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extends EF Core features.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreAlterationPersistenceFeature UseMySql(this EFCoreAlterationPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString, options);
        return feature;
    }
}
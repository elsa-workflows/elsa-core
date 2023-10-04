using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Alterations;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extends EF Core features.
/// </summary>
public static class AlterationsExtensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreAlterationPersistenceFeature UseMySql(this EFCoreAlterationPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static EFCoreAlterationPersistenceFeature UseSqlite(this EFCoreAlterationPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreAlterationPersistenceFeature UseSqlServer(this EFCoreAlterationPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreAlterationPersistenceFeature UsePostgreSql(this EFCoreAlterationPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
}
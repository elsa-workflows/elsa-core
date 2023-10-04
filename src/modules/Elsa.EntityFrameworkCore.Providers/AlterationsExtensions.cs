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
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UseMySql(this EFCoreAlterationsPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UseSqlite(this EFCoreAlterationsPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UseSqlServer(this EFCoreAlterationsPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UsePostgreSql(this EFCoreAlterationsPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(typeof(AlterationsExtensions).Assembly, connectionString, options);
        return feature;
    }
}
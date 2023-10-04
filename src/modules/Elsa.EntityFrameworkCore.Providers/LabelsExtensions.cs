using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Labels;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extends EF Core features.
/// </summary>
public static class LabelsExtensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UseMySql(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(typeof(LabelsExtensions).Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UseSqlite(this EFCoreLabelPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(typeof(LabelsExtensions).Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UseSqlServer(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(typeof(LabelsExtensions).Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UsePostgreSql(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(typeof(LabelsExtensions).Assembly, connectionString, options);
        return feature;
    }
}
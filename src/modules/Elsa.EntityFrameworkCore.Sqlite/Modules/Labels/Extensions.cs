using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Sqlite;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreLabelPersistenceFeature UseSqlite(this EFCoreLabelPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}
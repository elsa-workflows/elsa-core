using Elsa.EntityFrameworkCore.Modules.Labels;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreLabelPersistenceFeature UseMySql(this EFCoreLabelPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString);
        return feature;
    }
}
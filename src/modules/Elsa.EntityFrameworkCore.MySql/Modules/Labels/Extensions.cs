using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Common;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreLabelPersistenceFeature UseMySql(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString, options);
        return feature;
    }
}
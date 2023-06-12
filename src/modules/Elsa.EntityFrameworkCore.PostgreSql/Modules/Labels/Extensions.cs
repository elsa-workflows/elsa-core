using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Labels;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreLabelPersistenceFeature UsePostgreSql(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString, options);
        return feature;
    }
}
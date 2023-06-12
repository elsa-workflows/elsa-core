using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Labels;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreLabelPersistenceFeature UseSqlServer(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString, options);
        return feature;
    }
}
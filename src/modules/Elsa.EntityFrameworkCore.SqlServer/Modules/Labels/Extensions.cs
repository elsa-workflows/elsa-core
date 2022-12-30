using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Labels;

public static class Extensions
{
    public static EFCoreLabelPersistenceFeature UseSqlServer(this EFCoreLabelPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}
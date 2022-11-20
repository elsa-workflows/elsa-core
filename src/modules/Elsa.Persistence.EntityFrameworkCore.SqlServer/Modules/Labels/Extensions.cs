using Elsa.Persistence.EntityFrameworkCore.Modules.Labels;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.Labels;

public static class Extensions
{
    public static EFCoreLabelPersistenceFeature UseSqlServer(this EFCoreLabelPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}
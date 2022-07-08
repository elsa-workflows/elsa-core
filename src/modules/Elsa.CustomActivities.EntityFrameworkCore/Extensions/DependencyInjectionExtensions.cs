using Elsa.CustomActivities.EntityFrameworkCore.Features;
using Elsa.CustomActivities.Features;

namespace Elsa.CustomActivities.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static CustomActivitiesFeature UseEntityFrameworkCore(this CustomActivitiesFeature feature, Action<EFCoreCustomActivitiesPersistenceFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
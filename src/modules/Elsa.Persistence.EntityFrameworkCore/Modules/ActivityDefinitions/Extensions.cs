using Elsa.ActivityDefinitions.Features;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;

public static class Extensions
{
    public static ActivityDefinitionsFeature UseEntityFrameworkCore(this ActivityDefinitionsFeature feature, Action<EFCoreActivityDefinitionsPersistenceFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
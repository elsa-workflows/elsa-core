using Elsa.ActivityDefinitions.EntityFrameworkCore.Features;
using Elsa.ActivityDefinitions.Features;

namespace Elsa.ActivityDefinitions.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static ActivityDefinitionsFeature UseEntityFrameworkCore(this ActivityDefinitionsFeature feature, Action<EFCoreActivityDefinitionsPersistenceFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
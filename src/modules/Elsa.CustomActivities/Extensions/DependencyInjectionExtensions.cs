using Elsa.CustomActivities.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IModule UseCustomActivitiesApiEndpoints(this IModule module, Action<CustomActivitiesFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}
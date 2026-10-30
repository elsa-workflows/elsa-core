using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Resilience.Features;

namespace Elsa.Resilience.Extensions;

public static class ModuleExtensions
{
    public static IModule UseResilience(this IModule module, Action<ResilienceFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
using Elsa.AspNetCore.Features;
using Elsa.Features.Services;

namespace Elsa.AspNetCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseMvc(this IModule configuration, Action<MvcFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}
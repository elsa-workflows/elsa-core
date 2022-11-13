using Elsa.Dsl.Features;
using Elsa.Features.Services;

namespace Elsa.Dsl.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseDsl(this IModule configuration, Action<DslFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}
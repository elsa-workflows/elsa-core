using Elsa.Dsl.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseDsl(this IModule configuration, Action<DslFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}
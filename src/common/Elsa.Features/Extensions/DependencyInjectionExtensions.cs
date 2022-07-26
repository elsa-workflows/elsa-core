using Elsa.Features.Implementations;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Features.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule CreateModule(this IServiceCollection services) => new Module(services);

    public static IModule Use<T>(this IModule module, Action<T>? configure = default) where T: class, IFeature
    {
        module.Configure(configure);
        return module;
    }
}
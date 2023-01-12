using Elsa.Features.Implementations;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IServiceCollection"/> that creates and configures modules.  
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Returns a new instance of an <see cref="IModule"/> implementation.
    /// </summary>
    public static IModule CreateModule(this IServiceCollection services) => new Module(services);

    /// <summary>
    /// Installs and configures the specified feature. If the feature was already installed, it is not added twice, which means it is safe to call this method multiple times.
    /// </summary>
    public static IModule Use<T>(this IModule module, Action<T>? configure = default) where T: class, IFeature
    {
        module.Configure(configure);
        return module;
    }
}
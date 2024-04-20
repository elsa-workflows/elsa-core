using System.Collections.Concurrent;
using Elsa.Features;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods to <see cref="IServiceCollection"/>. 
/// </summary>
public static class ModuleExtensions
{
    private static readonly IDictionary<IServiceCollection, IModule> Modules = new ConcurrentDictionary<IServiceCollection, IModule>();
    
    /// <summary>
    /// Creates a new Elsa module and adds the <see cref="ElsaFeature"/> to it.
    /// </summary>
    public static IModule AddElsa(this IServiceCollection services, Action<IModule>? configure = default)
    {
        var module = services.GetOrCreateModule();
        module.Configure<AppFeature>(app => app.Configurator = configure);
        module.Apply();
        
        return module;
    }
    
    /// <summary>
    /// Configures the Elsa module.
    /// </summary>
    public static IModule ConfigureElsa(this IServiceCollection services, Action<IModule>? configure = default)
    {
        var module = services.GetOrCreateModule();
        
        if(configure != null)
            module.Configure<AppFeature>(app => app.Configurator += configure);
        
        return module;
    }
    
    private static IModule GetOrCreateModule(this IServiceCollection services)
    {
        if(Modules.TryGetValue(services, out var module))
            return module;
        
        module = services.CreateModule();
        
        Modules[services] = module;
        return module;
    }
}
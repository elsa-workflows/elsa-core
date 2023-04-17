using Elsa.Features;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods to <see cref="IServiceCollection"/>. 
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Creates a new Elsa module and adds the <see cref="ElsaFeature"/> to it.
    /// </summary>
    public static IModule AddElsa(this IServiceCollection services, Action<IModule>? configure = default)
    {
        var module = services.CreateModule();
        module.Configure<AppFeature>(app => app.Configurator = configure);
        module.Apply();
        return module;
    }
}
using Elsa.Secrets.Api.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.  
public static class ModuleExtensions
{
    /// Installs the Semantic Kernel API feature.
    public static IModule UseSecretsApi(this IModule module, Action<SecretsApiFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
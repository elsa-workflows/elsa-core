using Elsa.Agents.Api.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.  
public static class SemanticKernelApiModuleExtensions
{
    /// Installs the Semantic Kernel API feature.
    public static IModule UseSemanticKernelApi(this IModule module, Action<SemanticKernelApiFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
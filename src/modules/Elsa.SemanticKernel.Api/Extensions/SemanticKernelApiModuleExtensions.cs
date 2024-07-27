using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.SemanticKernel.Api.Features;

namespace Elsa.SemanticKernel.Api.Extensions;

/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.  
public static class SemanticKernelApiModuleExtensions
{
    /// Installs the Semantic Kernel API feature.
    public static IModule UseSemanticKernelApi(this IModule module, Action<SemanticKernelApiFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
using Elsa.Features.Services;
using Elsa.SemanticKernel.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// An extension class that installs the SemanticKernel feature.
[PublicAPI]
public static class SemanticKernelActivitiesFeatureModuleExtensions
{
    /// Installs the SemanticKernel feature.
    public static IModule UseSemanticKernel(this IModule module, Action<SemanticKernelFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
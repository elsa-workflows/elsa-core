using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to setup the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the <see cref="WorkflowRuntimeFeature"/> feature.
    /// </summary>
    public static IModule UseWorkflowRuntime(this IModule module, Action<WorkflowRuntimeFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}
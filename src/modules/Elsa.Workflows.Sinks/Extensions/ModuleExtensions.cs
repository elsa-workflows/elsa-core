using Elsa.Features.Services;
using Elsa.Workflows.Sinks.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides helpful extensions to install the workflow sinks feature into the module.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the workflow sinks feature of the specified module.
    /// </summary>
    public static IModule UseWorkflowSinks(this IModule module, Action<WorkflowSinksFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}
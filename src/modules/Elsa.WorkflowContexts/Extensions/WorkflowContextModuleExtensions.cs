using Elsa.Features.Services;
using Elsa.WorkflowContexts.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IModule"/> to add workflow context providers.
/// </summary>
[PublicAPI]
public static class WorkflowContextModuleExtensions
{
    /// <summary>
    /// Adds support for workflow context providers.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="configure">The configuration delegate.</param>
    public static IModule UseWorkflowContexts(this IModule module, Action<WorkflowContextsFeature>? configure = default)
    {
        module.Use(configure);
        module.Use<WorkflowContextsJavaScriptFeature>();
        return module;
    }
}
using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to the specified <see cref="IModule"/>/
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the workflow management feature to the specified module. 
    /// </summary>
    public static IModule UseWorkflowManagement(this IModule module, Action<WorkflowManagementFeature>? configure = default)
    {
        module.Configure<WorkflowManagementFeature>(management =>
        {
            management.AddActivity<NotFoundActivity>();
            configure?.Invoke(management);
        });
        return module;
    }
}
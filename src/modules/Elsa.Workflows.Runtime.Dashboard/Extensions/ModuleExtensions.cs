using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Dashboard.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowRuntimeDashboardModuleExtensions
{
    public static IModule UseWorkflowRuntimeDashboard(this IModule module, Action<WorkflowRuntimeDashboardFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}

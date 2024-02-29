using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Management.MassTransit.Features;

namespace Elsa.Workflows.Management.MassTransit.Extensions;

public static class ModuleExtensions
{
    public static IModule UseMassTransitDispatcher(this WorkflowManagementFeature workflowManagement, Action<MassTransitWorkflowManagementFeature>? configure = default)
    {
        return workflowManagement.Module.Use(configure);
    }
}
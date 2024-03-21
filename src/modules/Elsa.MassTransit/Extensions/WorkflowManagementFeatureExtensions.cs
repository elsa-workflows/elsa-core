using Elsa.MassTransit.Features;
using Elsa.Workflows.Management.Features;

namespace Elsa.MassTransit.Extensions;

public static class WorkflowManagementFeatureExtensions
{
    public static WorkflowManagementFeature UseMassTransitDispatcher(this WorkflowManagementFeature feature)
    {
        feature.Module.Configure<MassTransitWorkflowManagementFeature>();
        return feature;
    }
}
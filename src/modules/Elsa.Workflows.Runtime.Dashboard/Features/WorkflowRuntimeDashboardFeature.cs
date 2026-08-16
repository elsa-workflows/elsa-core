using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Dashboard.Extensions;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Workflows.Runtime.Dashboard.Features;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class WorkflowRuntimeDashboardFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddWorkflowRuntimeDashboard();
    }
}

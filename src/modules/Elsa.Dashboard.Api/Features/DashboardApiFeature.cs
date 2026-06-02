using Elsa.Dashboard.Api.Extensions;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Dashboard.Api.Features;

[DependsOn(typeof(WorkflowInstancesFeature))]
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class DashboardApiFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<DashboardApiFeature>();
    }

    public override void Apply()
    {
        Services.AddDashboardApiServices();
        Module.AddFastEndpointsFromModule();
    }
}

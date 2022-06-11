using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Features;
using Elsa.Workflows.Core.Features;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Persistence.Features;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Features;

[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(FlowchartFeature))]
[DependsOn(typeof(WorkflowPersistenceFeature))]
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(WorkflowManagementFeature))]
public class ElsaFeature : FeatureBase
{
    public ElsaFeature(IModule module) : base(module)
    {
    }
}
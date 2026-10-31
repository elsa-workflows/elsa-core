using Elsa.Connections.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Workflows.Admission.Features;

[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(ConnectionsFeature))]
public sealed class AdmissionFeature(IModule module) : FeatureBase(module)
{
    public AdmissionHostConfiguration? Configuration { get; set; }
    public override void Apply() => Services.AddDurableWorkflowAdmission(Configuration
        ?? throw new InvalidOperationException("Explicit isolated admission host configuration is required."));
}

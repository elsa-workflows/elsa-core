using CShells.Features;
using Elsa.Workflows.Runtime.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission.ShellFeatures;

[ShellFeature(DisplayName = "Durable Workflow Admission", Description = "Guarded isolated local event admission", DependsOn = [typeof(WorkflowRuntimeFeature)])]
public sealed class AdmissionFeature : IShellFeature
{
    public AdmissionHostConfiguration? Configuration { get; set; }
    public void ConfigureServices(IServiceCollection services) => services.AddDurableWorkflowAdmission(Configuration
        ?? throw new InvalidOperationException("Explicit isolated admission host configuration is required."));
}

using CShells.Features;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Workflows.Runtime.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ShellFeatures;

[ShellFeature(DependsOn = [
    nameof(WorkflowManagementFeature),
    nameof(WorkflowRuntimeFeature)
])]
public class ElsaFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
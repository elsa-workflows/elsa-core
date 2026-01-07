using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ShellFeatures;

[ShellFeature(DependsOn = [
    "WorkflowManagement",
    "WorkflowRuntime"
])]
public class ElsaFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
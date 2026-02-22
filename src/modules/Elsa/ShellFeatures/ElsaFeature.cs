using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ShellFeatures;

[ShellFeature(
    DisplayName = "Elsa Core",
    Description = "Core Elsa workflow system functionality",
    DependsOn = [
    "WorkflowManagement",
    "WorkflowRuntime"
])]
public class ElsaFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the shell-based feature provider to bridge shell features to the Elsa feature API
        services.AddSingleton<Features.Contracts.IInstalledFeatureProvider, Features.Services.ShellInstalledFeatureProvider>();
    }
}
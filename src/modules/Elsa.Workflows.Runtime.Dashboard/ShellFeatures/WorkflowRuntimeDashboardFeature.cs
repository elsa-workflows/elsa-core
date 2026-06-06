using CShells.Features;
using Elsa.Workflows.Runtime.Dashboard.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Dashboard.ShellFeatures;

[ShellFeature(
    DisplayName = "Workflow Runtime Dashboard",
    Description = "Provides workflow runtime dashboard contributions",
    DependsOn = [typeof(global::Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature), typeof(global::Elsa.Dashboard.Api.ShellFeatures.DashboardApiFeature)])]
[UsedImplicitly]
public class WorkflowRuntimeDashboardFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWorkflowRuntimeDashboard();
    }
}

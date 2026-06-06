using CShells.Features;
using Elsa.Dashboard.Api.ShellFeatures;
using Elsa.Workflows.Runtime.Dashboard.Extensions;
using Elsa.Workflows.Runtime.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Dashboard.ShellFeatures;

[ShellFeature(
    DisplayName = "Workflow Runtime Dashboard",
    Description = "Provides workflow runtime dashboard contributions",
    DependsOn = [typeof(WorkflowRuntimeFeature), typeof(DashboardApiFeature)])]
[UsedImplicitly]
public class WorkflowRuntimeDashboardFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWorkflowRuntimeDashboard();
    }
}

using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Workflows.Management.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.ShellFeatures;

/// <summary>
/// Shell feature for workflow contexts.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Contexts",
    Description = "Provides workflow context management functionality")]
[UsedImplicitly]
public class WorkflowContextsShellFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddActivitiesFrom<WorkflowContextsShellFeature>();
    }
}


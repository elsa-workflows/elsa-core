using CShells.Features;
using Elsa.Workflows.Management.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Activities.ShellFeatures;

/// <summary>
/// Shell feature for agents activities.
/// </summary>
[ShellFeature(
    DisplayName = "Agents Activities",
    Description = "Provides workflow activities for agent invocation",
    DependsOn = ["AgentsCore"])]
[UsedImplicitly]
public class AgentsActivitiesShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddActivitiesFrom<AgentsActivitiesShellFeature>();
    }
}


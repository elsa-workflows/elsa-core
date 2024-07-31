using Elsa.Agents.ActivityProviders;
using Elsa.Agents.HostedServices;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Features;

/// A feature that installs Semantic Kernel functionality.
[DependsOn(typeof(WorkflowManagementFeature))]
[UsedImplicitly]
public class AgentsFeature(IModule module) : FeatureBase(module)
{
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ConfigureAgents>(-2);
        Module.ConfigureHostedService<ConfigureAgentManager>(-1);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddActivityProvider<AgentActivityProvider>()
            .AddAgents()
            ;
    }
}
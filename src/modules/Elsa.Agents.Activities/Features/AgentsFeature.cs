using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Activities.Features;

/// A feature that installs Semantic Kernel functionality.
[DependsOn(typeof(WorkflowManagementFeature))]
[UsedImplicitly]
public class AgentsFeature(IModule module) : FeatureBase(module)
{
    public override void ConfigureHostedServices()
    {
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
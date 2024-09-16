using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Agents.Activities.Handlers;
using Elsa.Agents.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Activities.Features;

/// A feature that installs Semantic Kernel functionality.
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(AgentsFeature))]
[UsedImplicitly]
public class AgentActivitiesFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddActivityProvider<AgentActivityProvider>()
            .AddNotificationHandler<RefreshActivityRegistry>()
            ;
    }
}
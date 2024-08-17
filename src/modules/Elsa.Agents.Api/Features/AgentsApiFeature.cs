using Elsa.Agents.Management.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Features;

/// A feature that installs API endpoints to interact with skilled agents.
[DependsOn(typeof(AgentManagementFeature))]
[UsedImplicitly]
public class AgentsApiFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<AgentsApiFeature>();
    }
}
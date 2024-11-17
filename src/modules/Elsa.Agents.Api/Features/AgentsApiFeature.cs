using Elsa.Agents.Persistence.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Features;

/// <summary>
/// A feature that installs API endpoints to interact with skilled agents.
/// </summary>
[DependsOn(typeof(AgentPersistenceFeature))]
[UsedImplicitly]
public class AgentsApiFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<AgentsApiFeature>();
    }
}
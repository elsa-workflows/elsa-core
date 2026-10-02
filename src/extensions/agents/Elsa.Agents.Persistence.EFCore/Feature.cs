using Elsa.Persistence.EFCore;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Persistence.EFCore;

/// <summary>
/// Configures the default workflow runtime to use EF Core persistence providers.
/// </summary>
[DependsOn(typeof(AgentPersistenceFeature))]
public class EFCoreAgentPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreAgentPersistenceFeature, AgentsDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<AgentPersistenceFeature>(feature =>
        {
            feature.UseAgentStore(sp => sp.GetRequiredService<EFCoreAgentStore>());
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<AgentDefinition, EFCoreAgentStore>();
    }
}
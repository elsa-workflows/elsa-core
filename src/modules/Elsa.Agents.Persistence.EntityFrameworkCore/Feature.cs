using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Features;
using Elsa.EntityFrameworkCore;
using Elsa.EntityFrameworkCore.Contracts;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

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
            feature
                .UseApiKeyStore(sp => sp.GetRequiredService<EFCoreApiKeyStore>())
                .UseServiceStore(sp => sp.GetRequiredService<EFCoreServiceStore>())
                .UseAgentStore(sp => sp.GetRequiredService<EFCoreAgentStore>());
                ;
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<ApiKeyDefinition, EFCoreApiKeyStore>();
        AddEntityStore<ServiceDefinition, EFCoreServiceStore>();
        AddEntityStore<AgentDefinition, EFCoreAgentStore>();
        Services.AddScoped<IEntityModelCreatingHandler, SetupForOracle>();
    }
}
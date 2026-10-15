using Elsa.Agents.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Persistence.Features;

[DependsOn(typeof(AgentsCoreFeature))]
public class AgentPersistenceFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IAgentStore> _agentStoreFactory = sp => sp.GetRequiredService<MemoryAgentStore>();
    
    public AgentPersistenceFeature UseAgentStore(Func<IServiceProvider, IAgentStore> factory)
    {
        _agentStoreFactory = factory;
        return this;
    }

    public override void Configure()
    {
        Module.UseAgentsCore(agents => agents.UseKernelConfigProvider(sp => sp.GetRequiredService<StoreKernelConfigProvider>()));
    }

    public override void Apply()
    {
        Services.AddScoped(_agentStoreFactory);
        Services.AddScoped<IAgentManager, AgentManager>();
        Services.AddMemoryStore<AgentDefinition, MemoryAgentStore>();
        Services.AddScoped<StoreKernelConfigProvider>();
    }
}
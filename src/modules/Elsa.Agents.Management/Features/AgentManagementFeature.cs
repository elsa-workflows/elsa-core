using Elsa.Agents.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Management.Features;

[DependsOn(typeof(AgentsFeature))]
public class AgentManagementFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IApiKeyStore> _apiKeyStoreFactory = sp => sp.GetRequiredService<MemoryApiKeyStore>();
    private Func<IServiceProvider, IServiceStore> _serviceStoreFactory = sp => sp.GetRequiredService<MemoryServiceStore>();
    private Func<IServiceProvider, IAgentStore> _agentStoreFactory = sp => sp.GetRequiredService<MemoryAgentStore>();
    
    public AgentManagementFeature UseApiKeyStore(Func<IServiceProvider, IApiKeyStore> factory)
    {
        _apiKeyStoreFactory = factory;
        return this;
    }
    
    public AgentManagementFeature UseServiceStore(Func<IServiceProvider, IServiceStore> factory)
    {
        _serviceStoreFactory = factory;
        return this;
    }
    
    public AgentManagementFeature UseAgentStore(Func<IServiceProvider, IAgentStore> factory)
    {
        _agentStoreFactory = factory;
        return this;
    }

    public override void Configure()
    {
        Module.UseAgents(agents => agents.UseKernelConfigProvider(sp => sp.GetRequiredService<StoreKernelConfigProvider>()));
    }

    public override void Apply()
    {
        Services
            .AddScoped(_apiKeyStoreFactory)
            .AddScoped(_serviceStoreFactory)
            .AddScoped(_agentStoreFactory);

        Services
            .AddMemoryStore<ApiKeyDefinition, MemoryApiKeyStore>()
            .AddMemoryStore<ServiceDefinition, MemoryServiceStore>()
            .AddMemoryStore<AgentDefinition, MemoryAgentStore>();

        Services.AddScoped<StoreKernelConfigProvider>();
    }
}
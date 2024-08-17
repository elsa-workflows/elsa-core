using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Management.Features;

public class AgentManagementFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IApiKeyStore> ApiKeyStoreFactory => sp => sp.GetRequiredService<MemoryApiKeyStore>();
    private Func<IServiceProvider, IServiceStore> ServiceStoreFactory => sp => sp.GetRequiredService<MemoryServiceStore>();
    private Func<IServiceProvider, IAgentStore> AgentStoreFactory => sp => sp.GetRequiredService<MemoryAgentStore>();

    public override void Apply()
    {
        Services
            .AddScoped(ApiKeyStoreFactory)
            .AddScoped(ServiceStoreFactory)
            .AddScoped(AgentStoreFactory);

        Services
            .AddMemoryStore<ApiKeyDefinition, MemoryApiKeyStore>()
            .AddMemoryStore<ServiceDefinition, MemoryServiceStore>()
            .AddMemoryStore<AgentDefinition, MemoryAgentStore>();
    }
}
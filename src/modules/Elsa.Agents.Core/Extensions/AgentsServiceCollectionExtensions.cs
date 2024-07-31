using Elsa.Agents.HostedServices;
using Elsa.Agents.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents;

public static class AgentsServiceCollectionExtensions
{
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        services.AddOptions<AgentsOptions>();
        
        return services
                .AddSingleton<KernelFactory>()
                .AddSingleton<SkillExecutor>()
                .AddSingleton<AgentManager>()
                .AddSingleton<KernelConfig>()
                .AddHostedService<ConfigureAgents>()
                .AddHostedService<ConfigureAgentManager>()
            ;
    }
}
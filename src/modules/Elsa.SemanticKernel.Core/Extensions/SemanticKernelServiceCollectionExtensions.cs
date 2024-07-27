using Elsa.SemanticKernel.HostedServices;
using Elsa.SemanticKernel.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.SemanticKernel;

public static class SemanticKernelServiceCollectionExtensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services)
    {
        services.AddOptions<SemanticKernelOptions>();
        
        return services
                .AddSingleton<KernelFactory>()
                .AddSingleton<SkillExecutor>()
                .AddSingleton<AgentManager>()
                .AddSingleton<KernelConfig>()
                .AddHostedService<ConfigureSemanticKernel>()
                .AddHostedService<ConfigureAgentManager>()
            ;
    }
}
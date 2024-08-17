using Elsa.Agents.Options;
using Elsa.Agents.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        services.AddOptions<AgentsOptions>();
        
        return services
                .AddScoped<KernelFactory>()
                .AddScoped<AgentInvoker>()
                .AddScoped<IPluginsDiscoverer, PluginsDiscovererDiscoverer>()
                .AddScoped<IKernelConfigProvider, ConfigurationKernelConfigProvider>()
                .AddPluginProvider<ImageGeneratorPluginProvider>()
            ;
    }

    public static IServiceCollection AddPluginProvider<T>(this IServiceCollection services) where T: class, IPluginProvider
    {
        return services.AddScoped<IPluginProvider, T>();
    }
}
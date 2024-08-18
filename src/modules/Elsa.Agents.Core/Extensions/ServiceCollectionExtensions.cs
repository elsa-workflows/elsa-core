using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPluginProvider<T>(this IServiceCollection services) where T: class, IPluginProvider
    {
        return services.AddScoped<IPluginProvider, T>();
    }
    
    public static IServiceCollection AddAgentServiceProvider<T>(this IServiceCollection services) where T: class, IAgentServiceProvider
    {
        return services.AddScoped<IAgentServiceProvider, T>();
    }
}
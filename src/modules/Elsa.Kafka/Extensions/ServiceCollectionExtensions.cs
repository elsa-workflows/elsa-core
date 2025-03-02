using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Kafka;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumerFactory<T>(this IServiceCollection services) where T : class, IConsumerFactory
    {
        services.AddScoped<T>();
        return services;
    }
    
    public static IServiceCollection AddProducerFactory<T>(this IServiceCollection services) where T : class, IProducerFactory
    {
        services.AddScoped<T>();
        return services;
    }
}
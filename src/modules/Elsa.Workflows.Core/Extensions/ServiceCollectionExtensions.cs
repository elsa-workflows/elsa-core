using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageDriver<T>(this IServiceCollection services) where T : class, IStorageDriver
    {
        return services.AddScoped<IStorageDriver, T>();
    }
    
    public static IServiceCollection AddActivityStateFilter<T>(this IServiceCollection services) where T : class, IActivityStateFilter
    {
        return services.AddScoped<IActivityStateFilter, T>();
    }
}
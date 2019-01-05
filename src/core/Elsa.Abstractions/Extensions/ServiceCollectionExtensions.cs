using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityDescriptors<T>(this IServiceCollection services)
            where T : class, IActivityDescriptorProvider
        {
            services.TryAddSingleton<IActivityDescriptorProvider, T>();
            return services;
        }
        
        public static IServiceCollection AddActivityDriver<T>(this IServiceCollection services)
            where T : class, IActivityDriver
        {
            services.TryAddSingleton<IActivityDriver, T>();
            return services;
        }
    }
}
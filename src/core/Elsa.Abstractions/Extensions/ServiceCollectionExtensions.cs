using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityDescriptors<T>(this IServiceCollection services)
            where T : class, IActivityDescriptorProvider
        {
            services.AddSingleton<IActivityDescriptorProvider, T>();
            return services;
        }
        
        public static IServiceCollection AddActivityDriver<T>(this IServiceCollection services)
            where T : class, IActivityDriver
        {
            services.AddSingleton<T>();
            services.AddSingleton<IActivityDriver>(sp => sp.GetRequiredService<T>());
            return services;
        }
    }
}
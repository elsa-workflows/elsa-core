using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityDescriptors<T>(this IServiceCollection services)
            where T : class, IActivityDescriptorProvider
        {
            return services.AddSingleton<IActivityDescriptorProvider, T>();
        }
        
        public static IServiceCollection AddActivityDriver<T>(this IServiceCollection services)
            where T : class, IActivityDriver
        {
            return services.AddSingleton<IActivityDriver, T>();
        }
    }
}
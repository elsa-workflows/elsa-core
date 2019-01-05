using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityDescriptor<T>(this IServiceCollection services)
            where T : class, IActivityDescriptor
        {
            return services.AddSingleton<IActivityDescriptor, T>();
        }
        
        public static IServiceCollection AddActivityDriver<T>(this IServiceCollection services)
            where T : class, IActivityDriver
        {
            return services.AddSingleton<IActivityDriver, T>();
        }
    }
}
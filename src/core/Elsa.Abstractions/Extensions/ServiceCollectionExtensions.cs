using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityProvider<T>(this IServiceCollection services) where T : class, IActivityProvider => services.AddSingleton<IActivityProvider, T>();
        public static IServiceCollection AddActivityDesigners<T>(this IServiceCollection services) where T : class, IActivityDesignerProvider => services.AddSingleton<IActivityDesignerProvider, T>();

        public static IServiceCollection AddActivityDriver<T>(this IServiceCollection services)
            where T : class, IActivityDriver
        {
            return services
                .AddSingleton<T>()
                .AddSingleton<IActivityDriver>(sp => sp.GetRequiredService<T>());
        }
    }
}
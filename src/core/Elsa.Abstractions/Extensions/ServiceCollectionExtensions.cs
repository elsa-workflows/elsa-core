using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityDriver<T>(this IServiceCollection services)
            where T : class, IActivityDriver
        {
            return services
                .AddTransient<T>()
                .AddTransient<IActivityDriver>(sp => sp.GetRequiredService<T>());
        }
    }
}
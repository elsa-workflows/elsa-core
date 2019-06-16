using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivity<T>(this IServiceCollection services)
            where T : class, IActivity
        {
            return services
                .AddTransient<T>()
                .AddTransient<IActivity>(sp => sp.GetRequiredService<T>());
        }
    }
}
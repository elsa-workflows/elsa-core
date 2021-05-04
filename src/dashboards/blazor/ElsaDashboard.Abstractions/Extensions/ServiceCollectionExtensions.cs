using ElsaDashboard.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElsaDashboard.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityDisplayProvider<T>(this IServiceCollection services) where T : class, IActivityDisplayProvider
        {
            return services.AddTransient<IActivityDisplayProvider, T>();
        }
    }
}
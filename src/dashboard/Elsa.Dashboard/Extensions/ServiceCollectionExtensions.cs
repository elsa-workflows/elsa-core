using Elsa.Dashboard.Middleware;
using Elsa.Dashboard.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboard(this IServiceCollection services)
        {
            services.AddTransient<DashboardMiddleware>();
            services.ConfigureOptions<StaticAssetsConfigureOptions>();

            return services;
        }
    }
}
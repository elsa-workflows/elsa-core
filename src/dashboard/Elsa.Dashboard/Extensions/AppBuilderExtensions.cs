using Elsa.Dashboard.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Elsa.Dashboard.Extensions
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseElsaDashboard(this IApplicationBuilder app)
        {
            return app
                .UseStaticFiles()
                .Map("/elsa-dashboard", branch => branch.UseMiddleware<DashboardMiddleware>());
        }
    }
}
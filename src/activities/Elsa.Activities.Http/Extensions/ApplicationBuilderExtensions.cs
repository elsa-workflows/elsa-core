using Elsa.Activities.Http.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Activities.Http.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpWorkflows(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HttpRequestTriggerMiddleware>();
        }
    }
}
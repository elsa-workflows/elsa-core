using Elsa.Activities.Http.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Activities.Http.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app)
        {
            return app
                .UseMiddleware<HttpRequestTriggerMiddleware>()
                .Map("/workflows/signal", branch => branch.UseMiddleware<SignalMiddleware>());
        }
    }
}
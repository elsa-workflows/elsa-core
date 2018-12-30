using Flowsharp.Activities.Http.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Flowsharp.Activities.Http.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HttpRequestTriggerMiddleware>();
        }
    }
}
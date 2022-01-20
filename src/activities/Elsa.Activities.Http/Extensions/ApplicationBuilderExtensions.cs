using Elsa.Activities.Http.Middleware;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HttpEndpointMiddleware>();
        }
    }
}
using Elsa.Activities.Http.Middleware;
using Elsa.Activities.Http.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
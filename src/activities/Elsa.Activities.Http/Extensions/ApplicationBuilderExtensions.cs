using Elsa.Activities.Http.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            if (multiTenancyEnabled)
                return app.UseMiddleware<MultitenantHttpEndpointMiddleware>();
            else
                return app.UseMiddleware<HttpEndpointMiddleware>();
        }
    }
}
using Elsa.Http.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Http.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app) => app.UseMiddleware<HttpTriggerMiddleware>();
}
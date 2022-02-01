using Elsa.Modules.Http.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Modules.Http.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app) => app.UseMiddleware<HttpTriggerMiddleware>();
}
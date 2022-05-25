using Elsa.Workflows.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Workflows.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseJsonSerializationErrorHandler(this IApplicationBuilder app) => app.UseMiddleware<JsonSerializationErrorHandlerMiddleware>();
}
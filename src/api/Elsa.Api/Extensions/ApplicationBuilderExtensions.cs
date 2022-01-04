using Elsa.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseJsonSerializationErrorHandler(this IApplicationBuilder app) => app.UseMiddleware<JsonSerializationErrorHandlerMiddleware>();
}
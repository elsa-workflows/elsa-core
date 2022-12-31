using Elsa.Workflows.Api.Middleware;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseJsonSerializationErrorHandler(this IApplicationBuilder app) => app.UseMiddleware<JsonSerializationErrorHandlerMiddleware>();
}
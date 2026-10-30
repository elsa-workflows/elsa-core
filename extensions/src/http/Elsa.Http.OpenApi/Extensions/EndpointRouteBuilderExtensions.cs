using Elsa.Http.OpenApi.Contracts;
using Elsa.Http.OpenApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.OpenApi.Extensions;

/// <summary>
/// Extension methods for mapping OpenAPI endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps OpenAPI endpoints for workflow documentation.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapWorkflowOpenApi(this IEndpointRouteBuilder app)
    {
        // OpenAPI JSON endpoint
        app.MapGet("/openapi.json", async ([FromServices] IWorkflowEndpointExtractor extractor, [FromServices] IOpenApiGenerator generator) =>
        {
            var endpoints = await extractor.ExtractEndpointsAsync();
            var openApiJson = generator.GenerateOpenApiJson(endpoints);
            return Results.Content(openApiJson, "application/json");
        });

        // ReDoc UI endpoint
        app.MapGet("/documentation", () =>
        {
            var html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>API Documentation</title>
            </head>
            <body>
                <redoc spec-url=""/openapi.json""></redoc>
                <script src=""https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js""> </script>
            </body>
            </html>";
            return Results.Content(html, "text/html");
        });

        return app;
    }
}

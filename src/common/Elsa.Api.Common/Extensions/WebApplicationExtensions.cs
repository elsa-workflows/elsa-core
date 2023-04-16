using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods to add the FastEndpoints middleware configured for use with Elsa API endpoints. 
/// </summary>
[PublicAPI]
public static class WebApplicationExtensions
{
    /// <summary>
    /// Register the FastEndpoints middleware configured for use with with Elsa API endpoints.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="routePrefix">The route prefix to apply to Elsa API endpoints.</param>
    /// <example>E.g. "elsa/api" will expose endpoints like this: "/elsa/api/workflow-definitions"</example>
    public static IApplicationBuilder UseWorkflowsApi(this IApplicationBuilder app, string routePrefix = "elsa/api") =>
        app.UseFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = routePrefix;
            config.Serializer.RequestDeserializer = DeserializeRequestAsync;
            config.Serializer.ResponseSerializer = SerializeRequestAsync;
        });
    
    /// <summary>
    /// Register the FastEndpoints middleware configured for use with with Elsa API endpoints.
    /// </summary>
    /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to register the endpoints with.</param>
    /// <param name="routePrefix">The route prefix to apply to Elsa API endpoints.</param>
    /// /// <example>E.g. "elsa/api" will expose endpoints like this: "/elsa/api/workflow-definitions"</example>
    public static IEndpointRouteBuilder MapWorkflowsApi(this IEndpointRouteBuilder routes, string routePrefix = "elsa/api") =>
        routes.MapFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = routePrefix;
            config.Serializer.RequestDeserializer = DeserializeRequestAsync;
            config.Serializer.ResponseSerializer = SerializeRequestAsync;
        });

    private static ValueTask<object?> DeserializeRequestAsync(HttpRequest httpRequest, Type modelType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializer = httpRequest.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
        var options = serializer.CreateOptions();

        return serializerContext == null
            ? JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, options, cancellationToken)
            : JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, serializerContext, cancellationToken);
    }

    private static Task SerializeRequestAsync(HttpResponse httpResponse, object? dto, string contentType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializer = httpResponse.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
        var options = serializer.CreateOptions();

        httpResponse.ContentType = contentType;
        return serializerContext == null
            ? JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), options, cancellationToken)
            : JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), serializerContext, cancellationToken);
    }

}
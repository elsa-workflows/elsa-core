using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods to add the FastEndpoints middleware configured for use with Elsa API endpoints. 
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Register the FastEndpoints middleware configured for use with with Elsa API endpoints.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="routePrefix">The route prefix to apply to Elsa API endpoints.</param>
    /// <example>E.g. "elsa/api" will expose endpoints like this: "/elsa/api/workflow-definitions"</example>
    public static IApplicationBuilder UseElsaFastEndpoints(this IApplicationBuilder app, string routePrefix = "elsa/api") =>
        app.UseFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = routePrefix;
            config.Serializer.RequestDeserializer = DeserializeRequestAsync;
            config.Serializer.ResponseSerializer = SerializeRequestAsync;
        });

    private static ValueTask<object?> DeserializeRequestAsync(HttpRequest httpRequest, Type modelType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializerOptionsProvider = httpRequest.HttpContext.RequestServices.GetRequiredService<SerializerOptionsProvider>();
        var options = serializerOptionsProvider.CreateApiOptions();

        return serializerContext == null
            ? JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, options, cancellationToken)
            : JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, serializerContext, cancellationToken);
    }

    private static Task SerializeRequestAsync(HttpResponse httpResponse, object? dto, string contentType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializerOptionsProvider = httpResponse.HttpContext.RequestServices.GetRequiredService<SerializerOptionsProvider>();
        var options = serializerOptionsProvider.CreateApiOptions();

        httpResponse.ContentType = contentType;
        return serializerContext == null
            ? JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto.GetType(), options, cancellationToken)
            : JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto.GetType(), serializerContext, cancellationToken);
    }

}
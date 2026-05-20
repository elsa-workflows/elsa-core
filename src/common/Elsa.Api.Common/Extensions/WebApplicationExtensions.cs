using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
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
    private static readonly object OriginalEndpointKey = new();

    /// <summary>
    /// Register the FastEndpoints middleware configured for use with with Elsa API endpoints.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="routePrefix">The route prefix to apply to Elsa API endpoints.</param>
    /// <example>E.g. "elsa/api" will expose endpoints like this: "/elsa/api/workflow-definitions"</example>
    public static IApplicationBuilder UseWorkflowsApi(this IApplicationBuilder app, string routePrefix = "elsa/api")
    {
        return app.UseFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = routePrefix;
            config.Serializer.RequestDeserializer = DeserializeRequestAsync;
            config.Serializer.ResponseSerializer = SerializeRequestAsync;

            config.Binding.ValueParserFor<DateTimeOffset>(s =>
                new(DateTimeOffset.TryParse(s.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result), result));
        });
    }

    /// <summary>
    /// Applies an ASP.NET Core rate limiting policy to requests targeting the Elsa API route prefix.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="routePrefix">The route prefix used by Elsa API endpoints.</param>
    /// <param name="policyName">The registered ASP.NET Core rate limiting policy name. Leave empty to skip rate limiting.</param>
    public static IApplicationBuilder UseWorkflowsApiRateLimiting(this IApplicationBuilder app, string routePrefix = "elsa/api", string? policyName = null)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            return app;

        var pathPrefix = "/" + routePrefix.Trim('/');
        return app.UseRateLimitingPolicyForPath(pathPrefix, policyName, "Elsa API rate limiting endpoint");
    }

    /// <summary>
    /// Register the FastEndpoints middleware configured for use with with Elsa API endpoints.
    /// </summary>
    /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to register the endpoints with.</param>
    /// <param name="routePrefix">The route prefix to apply to Elsa API endpoints.</param>
    /// <example>E.g. "elsa/api" will expose endpoints like this: "/elsa/api/workflow-definitions"</example>
    public static IEndpointRouteBuilder MapWorkflowsApi(this IEndpointRouteBuilder routes, string routePrefix = "elsa/api") =>
        routes.MapFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = routePrefix;
            config.Serializer.RequestDeserializer = DeserializeRequestAsync;
            config.Serializer.ResponseSerializer = SerializeRequestAsync;
        });

    /// <summary>
    /// Applies an ASP.NET Core rate limiting policy to requests targeting the specified path prefix.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="pathPrefix">The path prefix to protect.</param>
    /// <param name="policyName">The registered ASP.NET Core rate limiting policy name.</param>
    /// <param name="displayName">The endpoint display name used for rate limiting metadata.</param>
    public static IApplicationBuilder UseRateLimitingPolicyForPath(this IApplicationBuilder app, PathString pathPrefix, string policyName, string displayName)
    {
        if (!pathPrefix.HasValue)
            return app;

        return app.UseWhen(
            context => context.Request.Path.StartsWithSegments(pathPrefix, StringComparison.OrdinalIgnoreCase),
            branch =>
            {
                branch.Use((context, next) =>
                {
                    context.Items[OriginalEndpointKey] = context.GetEndpoint();
                    context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(policyName)), displayName));
                    return next(context);
                });
                branch.UseRateLimiter();
                branch.Use((context, next) =>
                {
                    if (context.Items.TryGetValue(OriginalEndpointKey, out var endpoint))
                        context.SetEndpoint((Endpoint?)endpoint);

                    return next(context);
                });
            });
    }

    private static ValueTask<object?> DeserializeRequestAsync(HttpRequest httpRequest, Type modelType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializer = httpRequest.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
        var options = serializer.GetOptions();

        return serializerContext == null
            ? JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, options, cancellationToken)
            : JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, serializerContext, cancellationToken);
    }

    private static Task SerializeRequestAsync(HttpResponse httpResponse, object? dto, string contentType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializer = httpResponse.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
        var options = serializer.GetOptions();

        httpResponse.ContentType = contentType;
        return serializerContext == null
            ? JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), options, cancellationToken)
            : JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), serializerContext, cancellationToken);
    }

}

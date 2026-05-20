using System.Globalization;
using System.Runtime.CompilerServices;
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
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods to add the FastEndpoints middleware configured for use with Elsa API endpoints. 
/// </summary>
[PublicAPI]
public static class WebApplicationExtensions
{
    private const string PolicyMapPropertyName = "PolicyMap";
    private const string RateLimitingMetricsTypeName = "Microsoft.AspNetCore.RateLimiting.RateLimitingMetrics";
    private const string UnactivatedPolicyMapPropertyName = "UnactivatedPolicyMap";

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
    /// <remarks>
    /// This method only attaches rate limiting metadata. In endpoint-routed pipelines, call this after routing has selected an endpoint
    /// and before the host's single <c>app.UseRateLimiter()</c> middleware.
    /// </remarks>
    public static IApplicationBuilder UseRateLimitingPolicyForPath(this IApplicationBuilder app, PathString pathPrefix, string policyName, string displayName)
    {
        if (!pathPrefix.HasValue || string.IsNullOrWhiteSpace(policyName))
            return app;

        ValidateRateLimiterPolicy(app, policyName);
        var rateLimitingMetadata = new EnableRateLimitingAttribute(policyName);
        var fallbackEndpoint = CreateRateLimitingEndpoint(null, rateLimitingMetadata, displayName);
        var endpointCache = new ConditionalWeakTable<Endpoint, Endpoint>();

        return app.UseWhen(
            context => context.Request.Path.StartsWithSegments(pathPrefix, StringComparison.OrdinalIgnoreCase),
            branch =>
            {
                branch.Use(async (context, next) =>
                {
                    var originalEndpoint = context.GetEndpoint();
                    var rateLimitingEndpoint = originalEndpoint == null
                        ? fallbackEndpoint
                        : endpointCache.GetValue(originalEndpoint, endpoint => CreateRateLimitingEndpoint(endpoint, rateLimitingMetadata, displayName));

                    context.SetEndpoint(rateLimitingEndpoint);

                    try
                    {
                        await next(context);
                    }
                    finally
                    {
                        context.SetEndpoint(originalEndpoint);
                    }
                });
            });
    }

    private static void ValidateRateLimiterPolicy(IApplicationBuilder app, string policyName)
    {
        if (!HasRateLimiterServices(app.ApplicationServices))
            throw new InvalidOperationException($"Rate limiting services are not registered. Call services.AddRateLimiter(...) before applying Elsa rate limiting middleware for policy '{policyName}'.");

        var options = app.ApplicationServices.GetService<IOptions<RateLimiterOptions>>()?.Value;

        if (options == null)
            throw new InvalidOperationException($"Rate limiting services are not registered. Call services.AddRateLimiter(...) before applying Elsa rate limiting middleware for policy '{policyName}'.");

        var policyFound = TryHasRateLimiterPolicy(options, policyName);

        if (policyFound == false)
            throw new InvalidOperationException($"Rate limiting policy '{policyName}' is not registered, or rate limiting services were not registered. Call services.AddRateLimiter(...) and register the policy before applying Elsa rate limiting middleware.");
    }

    private static bool HasRateLimiterServices(IServiceProvider serviceProvider)
    {
        // IOptions<RateLimiterOptions> can exist without AddRateLimiter(); this internal service is registered by AddRateLimiter().
        var metricsType = typeof(RateLimiterOptions).Assembly.GetType(RateLimitingMetricsTypeName);
        return metricsType == null || serviceProvider.GetService(metricsType) != null;
    }

    private static bool? TryHasRateLimiterPolicy(RateLimiterOptions options, string policyName)
    {
        var results = new[]
        {
            TryContainsPolicy(options, PolicyMapPropertyName, policyName),
            TryContainsPolicy(options, UnactivatedPolicyMapPropertyName, policyName)
        };

        if (results.Any(result => result == true))
            return true;

        return results.Any(result => !result.HasValue) ? null : false;
    }

    private static Endpoint CreateRateLimitingEndpoint(Endpoint? originalEndpoint, EnableRateLimitingAttribute rateLimitingMetadata, string displayName)
    {
        var metadata = originalEndpoint == null
            ? new EndpointMetadataCollection(rateLimitingMetadata)
            : new EndpointMetadataCollection(originalEndpoint.Metadata.Where(x => x is not EnableRateLimitingAttribute and not DisableRateLimitingAttribute).Concat([rateLimitingMetadata]));

        if (originalEndpoint is RouteEndpoint routeEndpoint)
            return new RouteEndpoint(routeEndpoint.RequestDelegate ?? (_ => Task.CompletedTask), routeEndpoint.RoutePattern, routeEndpoint.Order, metadata, routeEndpoint.DisplayName ?? displayName);

        return new Endpoint(originalEndpoint?.RequestDelegate ?? (_ => Task.CompletedTask), metadata, originalEndpoint?.DisplayName ?? displayName);
    }

    private static bool? TryContainsPolicy(RateLimiterOptions options, string propertyName, string policyName)
    {
        var property = typeof(RateLimiterOptions).GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (property == null)
            return null;

        var map = property.GetValue(options);
        var containsKey = map?.GetType().GetMethod(nameof(Dictionary<string, object>.ContainsKey), [typeof(string)]);

        return containsKey?.Invoke(map, [policyName]) is bool result ? result : null;
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

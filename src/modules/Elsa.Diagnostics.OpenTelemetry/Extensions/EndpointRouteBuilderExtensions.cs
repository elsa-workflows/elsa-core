using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Ingestion;
using Elsa.Diagnostics.OpenTelemetry.Ingestion.HttpProtobuf;
using Elsa.Diagnostics.OpenTelemetry.RealTime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public const string DefaultHubRoute = "/elsa/hubs/diagnostics/opentelemetry";

    public static void MapOpenTelemetryHub(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OpenTelemetryDiagnosticsOptions>>().Value;
        endpoints.MapHub<OpenTelemetryHub>(string.IsNullOrWhiteSpace(options.HubRoute) ? DefaultHubRoute : options.HubRoute);
    }

    public static void MapOpenTelemetryHttpProtobufCollector(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OpenTelemetryDiagnosticsOptions>>().Value;
        var basePath = (string.IsNullOrWhiteSpace(options.HttpEndpointPath) ? "/elsa/otlp/v1" : options.HttpEndpointPath).TrimEnd('/');

        endpoints.MapPost($"{basePath}/traces", static async (HttpContext httpContext, IOpenTelemetryIngestor ingestor, IOptions<OpenTelemetryDiagnosticsOptions> options, CancellationToken cancellationToken) =>
        {
            if (!OtlpIngestionSecurity.IsAuthorized(httpContext, options.Value))
                return Results.Unauthorized();

            var payload = await ReadBodyAsync(httpContext, cancellationToken);
            await ingestor.IngestAsync(OtlpHttpProtobufParser.ParseTraces(payload.Span), cancellationToken);
            return Results.Ok();
        });

        endpoints.MapPost($"{basePath}/metrics", static async (HttpContext httpContext, IOpenTelemetryIngestor ingestor, IOptions<OpenTelemetryDiagnosticsOptions> options, CancellationToken cancellationToken) =>
        {
            if (!OtlpIngestionSecurity.IsAuthorized(httpContext, options.Value))
                return Results.Unauthorized();

            var payload = await ReadBodyAsync(httpContext, cancellationToken);
            await ingestor.IngestAsync(OtlpHttpProtobufParser.ParseMetrics(payload.Span), cancellationToken);
            return Results.Ok();
        });

        endpoints.MapPost($"{basePath}/logs", static async (HttpContext httpContext, IOpenTelemetryIngestor ingestor, IOptions<OpenTelemetryDiagnosticsOptions> options, CancellationToken cancellationToken) =>
        {
            if (!OtlpIngestionSecurity.IsAuthorized(httpContext, options.Value))
                return Results.Unauthorized();

            var payload = await ReadBodyAsync(httpContext, cancellationToken);
            await ingestor.IngestAsync(OtlpHttpProtobufParser.ParseLogs(payload.Span), cancellationToken);
            return Results.Ok();
        });
    }

    public static void MapOpenTelemetryGrpcCollector(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OpenTelemetryDiagnosticsOptions>>().Value;

        if (!options.EnableGrpc)
            return;

        if (string.IsNullOrWhiteSpace(options.GrpcEndpointPath))
            throw new InvalidOperationException("OpenTelemetry gRPC ingestion is enabled, but no gRPC endpoint path was configured.");

        // The actual gRPC service binding is host-specific. This module exposes shared ingestion
        // contracts and accurate collector metadata without forcing every host to reference gRPC.
    }

    private static async Task<ReadOnlyMemory<byte>> ReadBodyAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await httpContext.Request.Body.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

}

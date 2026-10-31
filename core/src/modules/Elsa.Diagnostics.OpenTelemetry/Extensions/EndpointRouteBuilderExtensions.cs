using System.Buffers;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Ingestion;
using Elsa.Diagnostics.OpenTelemetry.Ingestion.HttpProtobuf;
using Elsa.Diagnostics.OpenTelemetry.Models;
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
            return await IngestAsync(httpContext, ingestor, options, payload => OtlpHttpProtobufParser.ParseTraces(payload.Span), cancellationToken);
        });

        endpoints.MapPost($"{basePath}/metrics", static async (HttpContext httpContext, IOpenTelemetryIngestor ingestor, IOptions<OpenTelemetryDiagnosticsOptions> options, CancellationToken cancellationToken) =>
        {
            return await IngestAsync(httpContext, ingestor, options, payload => OtlpHttpProtobufParser.ParseMetrics(payload.Span), cancellationToken);
        });

        endpoints.MapPost($"{basePath}/logs", static async (HttpContext httpContext, IOpenTelemetryIngestor ingestor, IOptions<OpenTelemetryDiagnosticsOptions> options, CancellationToken cancellationToken) =>
        {
            return await IngestAsync(httpContext, ingestor, options, payload => OtlpHttpProtobufParser.ParseLogs(payload.Span), cancellationToken);
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

    private static async Task<IResult> IngestAsync(
        HttpContext httpContext,
        IOpenTelemetryIngestor ingestor,
        IOptions<OpenTelemetryDiagnosticsOptions> options,
        Func<ReadOnlyMemory<byte>, OpenTelemetryBatch> parse,
        CancellationToken cancellationToken)
    {
        if (!OtlpIngestionSecurity.IsAuthorized(httpContext, options.Value))
            return Results.Unauthorized();

        try
        {
            var payload = await ReadBodyAsync(httpContext, options.Value.MaxHttpRequestBodySize, cancellationToken);
            await ingestor.IngestAsync(parse(payload), cancellationToken);
            return Results.Ok();
        }
        catch (RequestBodyTooLargeException)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }
        catch (InvalidDataException)
        {
            return Results.BadRequest();
        }
    }

    private static async Task<ReadOnlyMemory<byte>> ReadBodyAsync(HttpContext httpContext, long maxBodySize, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        var totalBytes = 0L;

        try
        {
            int read;
            while ((read = await httpContext.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                totalBytes += read;
                if (totalBytes > maxBodySize)
                    throw new RequestBodyTooLargeException();

                stream.Write(buffer, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return stream.ToArray();
    }

    private sealed class RequestBodyTooLargeException : Exception;
}

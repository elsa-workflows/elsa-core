using System.Reflection;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Features;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using FastEndpoints;

namespace Elsa.Diagnostics.OpenTelemetry.IntegrationTests;

public class OpenTelemetryAuthorizationTests
{
    [Theory]
    [InlineData("Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Resources.Endpoint")]
    [InlineData("Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Traces.Endpoint")]
    [InlineData("Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Trace.Endpoint")]
    [InlineData("Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Metrics.Endpoint")]
    [InlineData("Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Logs.Endpoint")]
    [InlineData("Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Storage.Endpoint")]
    [InlineData("Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.CollectorConfiguration.Endpoint")]
    public void RestEndpoints_RequireOpenTelemetryReadPermission(string endpointTypeName)
    {
        var permissions = GetConfiguredPermissions(endpointTypeName);

        Assert.Contains(OpenTelemetryPermissions.Read, permissions);
    }

    private static IReadOnlyCollection<string> GetConfiguredPermissions(string endpointTypeName)
    {
        var endpointType = typeof(OpenTelemetryFeature).Assembly.GetType(endpointTypeName, throwOnError: true)!;
        var endpoint = Activator.CreateInstance(endpointType, new TestOpenTelemetryProvider())!;
        var (requestDtoType, responseDtoType) = GetEndpointDtoTypes(endpointType);
        var definition = new EndpointDefinition(endpointType, requestDtoType, responseDtoType);

        endpointType
            .GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, definition);

        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        var permissions = definition
            .GetType()
            .GetProperty("AllowedPermissions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(definition);

        return Assert.IsAssignableFrom<IEnumerable<string>>(permissions).ToArray();
    }

    private static (Type RequestDtoType, Type ResponseDtoType) GetEndpointDtoTypes(Type endpointType)
    {
        var type = endpointType;

        while (type.BaseType != null)
        {
            type = type.BaseType;

            if (!type.IsGenericType)
                continue;

            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            if (genericTypeDefinition == typeof(Elsa.Abstractions.ElsaEndpoint<,>))
                return (genericArguments[0], genericArguments[1]);

            if (genericTypeDefinition == typeof(Elsa.Abstractions.ElsaEndpoint<,,>))
                return (genericArguments[0], genericArguments[1]);

            if (genericTypeDefinition == typeof(Elsa.Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), genericArguments[0]);
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
    }

    private class TestOpenTelemetryProvider : IOpenTelemetryProvider
    {
        public ValueTask<OpenTelemetryResourceResult> GetResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(new OpenTelemetryResourceResult([], 0));

        public ValueTask<OpenTelemetryTraceResult> GetTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(new OpenTelemetryTraceResult([], 0));

        public ValueTask<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default) => ValueTask.FromResult<OpenTelemetryTraceDetail?>(null);

        public ValueTask<OpenTelemetryMetricResult> GetMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(new OpenTelemetryMetricResult([], [], 0));

        public ValueTask<OpenTelemetryLogResult> GetLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(new OpenTelemetryLogResult([], 0));

        public ValueTask<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(new OpenTelemetryStorageDiagnostics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

        public ValueTask<CollectorConfiguration> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var configuration = new CollectorConfiguration(
                new CollectorEndpointInfo("http/protobuf", "/elsa/otlp/v1", true, null),
                new CollectorEndpointInfo("grpc", null, false, "Disabled"),
                "OTEL_SERVICE_NAME",
                "OTEL_EXPORTER_OTLP_ENDPOINT",
                "OTEL_EXPORTER_OTLP_PROTOCOL",
                new Dictionary<string, string>());

            return ValueTask.FromResult(configuration);
        }
    }
}

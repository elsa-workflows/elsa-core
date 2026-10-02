using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

public record CollectorConfigurationRow(string Label, string Value);

public record CollectorEnvironmentVariable(string Name, string Value, bool IsSecret = false);

public record CollectorConfigurationViewModel(
    IReadOnlyCollection<CollectorConfigurationRow> Rows,
    IReadOnlyCollection<CollectorEnvironmentVariable> EnvironmentVariables);

public static class CollectorConfigurationViewModelMapper
{
    private const string MissingEndpoint = "Not configured";
    private const string MissingServiceName = "elsa-service";

    public static CollectorConfigurationViewModel Create(CollectorConfiguration configuration)
    {
        var preferredEndpoint = configuration.Http.Enabled
            ? configuration.Http
            : configuration.Grpc;

        var rows = new[]
        {
            new CollectorConfigurationRow("HTTP Endpoint", FormatEndpoint(configuration.Http)),
            new CollectorConfigurationRow("HTTP Protocol", configuration.Http.Protocol),
            new CollectorConfigurationRow("gRPC Endpoint", FormatEndpoint(configuration.Grpc)),
            new CollectorConfigurationRow("gRPC Protocol", configuration.Grpc.Protocol),
            new CollectorConfigurationRow("Endpoint Variable", configuration.EndpointEnvironmentVariable),
            new CollectorConfigurationRow("Protocol Variable", configuration.ProtocolEnvironmentVariable)
        };

        var environmentVariables = new List<CollectorEnvironmentVariable>
        {
            new(configuration.ServiceNameEnvironmentVariable, MissingServiceName),
            new(configuration.EndpointEnvironmentVariable, preferredEndpoint.Endpoint ?? MissingEndpoint),
            new(configuration.ProtocolEnvironmentVariable, preferredEndpoint.Protocol)
        };

        environmentVariables.AddRange(configuration.RequiredHeaders
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new CollectorEnvironmentVariable(x.Key, x.Value, IsSecretHeader(x.Key))));

        return new CollectorConfigurationViewModel(rows, environmentVariables);
    }

    private static string FormatEndpoint(CollectorEndpointInfo endpoint)
    {
        if (endpoint.Enabled)
            return endpoint.Endpoint ?? MissingEndpoint;

        return string.IsNullOrWhiteSpace(endpoint.DisabledReason)
            ? "Disabled"
            : endpoint.DisabledReason;
    }

    private static bool IsSecretHeader(string name)
    {
        return name.Contains("key", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("secret", StringComparison.OrdinalIgnoreCase);
    }
}

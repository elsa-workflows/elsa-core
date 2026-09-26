using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class CollectorConfigurationViewModelTests
{
    private readonly CollectorConfiguration _configuration = new(
        Http: new CollectorEndpointInfo("http/protobuf", "http://localhost:4318", true, null),
        Grpc: new CollectorEndpointInfo("grpc", null, false, "gRPC collector is disabled"),
        ServiceNameEnvironmentVariable: "OTEL_SERVICE_NAME",
        EndpointEnvironmentVariable: "OTEL_EXPORTER_OTLP_ENDPOINT",
        ProtocolEnvironmentVariable: "OTEL_EXPORTER_OTLP_PROTOCOL",
        RequiredHeaders: new Dictionary<string, string>
        {
            ["x-otlp-api-key"] = "secret-value",
            ["x-tenant"] = "tenant-a"
        });

    [Fact]
    public void Create_WhenHttpEndpointIsEnabled_UsesHttpEndpointForEnvironmentExample()
    {
        var viewModel = CollectorConfigurationViewModelMapper.Create(_configuration);

        var endpoint = Assert.Single(viewModel.EnvironmentVariables, x => x.Name == "OTEL_EXPORTER_OTLP_ENDPOINT");
        var protocol = Assert.Single(viewModel.EnvironmentVariables, x => x.Name == "OTEL_EXPORTER_OTLP_PROTOCOL");

        Assert.Equal("http://localhost:4318", endpoint.Value);
        Assert.Equal("http/protobuf", protocol.Value);
    }

    [Fact]
    public void Create_WhenGrpcEndpointIsDisabled_UsesDisabledReasonInRows()
    {
        var viewModel = CollectorConfigurationViewModelMapper.Create(_configuration);

        var grpcEndpoint = Assert.Single(viewModel.Rows, x => x.Label == "gRPC Endpoint");

        Assert.Equal("gRPC collector is disabled", grpcEndpoint.Value);
    }

    [Fact]
    public void Create_WhenHeadersContainSecrets_MarksSecretHeaders()
    {
        var viewModel = CollectorConfigurationViewModelMapper.Create(_configuration);

        var secretHeader = Assert.Single(viewModel.EnvironmentVariables, x => x.Name == "x-otlp-api-key");
        var publicHeader = Assert.Single(viewModel.EnvironmentVariables, x => x.Name == "x-tenant");

        Assert.True(secretHeader.IsSecret);
        Assert.False(publicHeader.IsSecret);
    }

    [Fact]
    public void Create_WhenHttpEndpointIsDisabled_FallsBackToGrpcEndpoint()
    {
        var configuration = _configuration with
        {
            Http = new CollectorEndpointInfo("http/protobuf", null, false, "HTTP collector is disabled"),
            Grpc = new CollectorEndpointInfo("grpc", "http://localhost:4317", true, null)
        };

        var viewModel = CollectorConfigurationViewModelMapper.Create(configuration);

        var endpoint = Assert.Single(viewModel.EnvironmentVariables, x => x.Name == "OTEL_EXPORTER_OTLP_ENDPOINT");
        var protocol = Assert.Single(viewModel.EnvironmentVariables, x => x.Name == "OTEL_EXPORTER_OTLP_PROTOCOL");

        Assert.Equal("http://localhost:4317", endpoint.Value);
        Assert.Equal("grpc", protocol.Value);
    }
}

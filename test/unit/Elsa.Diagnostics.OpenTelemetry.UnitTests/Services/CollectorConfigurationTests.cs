using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Services;

public class CollectorConfigurationTests
{
    [Fact]
    public async Task GetAsync_WhenGrpcIsDisabled_ReturnsDisabledGrpcMetadataWithoutSecretValue()
    {
        var provider = new CollectorConfigurationProvider(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions
        {
            ApiKey = "secret-value",
            EnableGrpc = false
        }));

        var configuration = await provider.GetAsync();

        Assert.True(configuration.Http.Enabled);
        Assert.False(configuration.Grpc.Enabled);
        Assert.Null(configuration.Grpc.Endpoint);
        Assert.Equal("<configured>", configuration.RequiredHeaders["x-otlp-api-key"]);
    }

    [Fact]
    public async Task GetAsync_WhenGrpcIsEnabled_ReturnsConfiguredGrpcEndpoint()
    {
        var provider = new CollectorConfigurationProvider(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions
        {
            EnableGrpc = true,
            GrpcEndpointPath = "https://localhost:4317"
        }));

        var configuration = await provider.GetAsync();

        Assert.True(configuration.Grpc.Enabled);
        Assert.Equal("https://localhost:4317", configuration.Grpc.Endpoint);
        Assert.Null(configuration.Grpc.DisabledReason);
    }
}

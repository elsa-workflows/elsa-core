using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Services;

public class CollectorConfigurationTests
{
    [Test]
    public async Task GetAsync_WhenGrpcIsDisabled_ReturnsDisabledGrpcMetadataWithoutSecretValue()
    {
        var provider = new CollectorConfigurationProvider(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions
        {
            ApiKey = "secret-value",
            EnableGrpc = false
        }));

        var configuration = await provider.GetAsync();

        await Assert.That(configuration.Http.Enabled).IsTrue();
        await Assert.That(configuration.Grpc.Enabled).IsFalse();
        await Assert.That(configuration.Grpc.Endpoint).IsNull();
        await Assert.That(configuration.RequiredHeaders["x-otlp-api-key"]).IsEqualTo("<configured>");
    }

    [Test]
    public async Task GetAsync_WhenGrpcIsEnabled_ReturnsConfiguredGrpcEndpoint()
    {
        var provider = new CollectorConfigurationProvider(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions
        {
            EnableGrpc = true,
            GrpcEndpointPath = "https://localhost:4317"
        }));

        var configuration = await provider.GetAsync();

        await Assert.That(configuration.Grpc.Enabled).IsTrue();
        await Assert.That(configuration.Grpc.Endpoint).IsEqualTo("https://localhost:4317");
        await Assert.That(configuration.Grpc.DisabledReason).IsNull();
    }
}
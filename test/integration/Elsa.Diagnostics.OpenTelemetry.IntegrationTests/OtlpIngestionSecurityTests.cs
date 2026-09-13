using System.Net;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.OpenTelemetry.IntegrationTests;

public class OtlpIngestionSecurityTests : OpenTelemetryWebApplicationTest
{
    [Test]
    public async Task PostTraces_WhenLoopbackAndNoApiKey_AllowsDevelopmentIngestion()
    {
        using var client = Factory.CreateClient();
        using var content = CreateEmptyProtobufContent();

        using var response = await client.PostAsync("/elsa/otlp/v1/traces", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostTraces_WhenNonLoopbackAndNoApiKey_RejectsIngestion()
    {
        using var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/elsa/otlp/v1/traces")
        {
            Content = CreateEmptyProtobufContent()
        };
        request.Headers.Add(OpenTelemetryWebApplicationFactory.TestRemoteIpAddressHeaderName, "10.0.0.5");

        using var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostTraces_WhenApiKeyMatches_AllowsNonLoopbackIngestion()
    {
        Services.GetRequiredService<IOptions<OpenTelemetryDiagnosticsOptions>>().Value.ApiKey = "secret";
        using var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/elsa/otlp/v1/traces")
        {
            Content = CreateEmptyProtobufContent()
        };
        request.Headers.Add(OpenTelemetryWebApplicationFactory.TestRemoteIpAddressHeaderName, "10.0.0.5");
        request.Headers.Add("x-otlp-api-key", "secret");

        using var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task CollectorConfiguration_WhenApiKeyIsConfigured_DoesNotExposeSecret()
    {
        var provider = new CollectorConfigurationProvider(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions { ApiKey = "secret" }));

        var configuration = await provider.GetAsync();

        await Assert.That(configuration.RequiredHeaders["x-otlp-api-key"]).IsEqualTo("<configured>");
        await Assert.That(configuration.RequiredHeaders.Values).DoesNotContain("secret");
    }

    private static ByteArrayContent CreateEmptyProtobufContent()
    {
        var content = new ByteArrayContent([]);
        content.Headers.ContentType = new("application/x-protobuf");
        return content;
    }
}

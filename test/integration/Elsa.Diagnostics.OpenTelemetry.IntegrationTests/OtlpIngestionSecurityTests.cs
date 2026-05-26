using System.Net;
using Elsa.Diagnostics.OpenTelemetry.Extensions;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.OpenTelemetry.IntegrationTests;

public class OtlpIngestionSecurityTests
{
    [Fact]
    public async Task PostTraces_WhenLoopbackAndNoApiKey_AllowsDevelopmentIngestion()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/elsa/otlp/v1/traces", CreateEmptyProtobufContent());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostTraces_WhenNonLoopbackAndNoApiKey_RejectsIngestion()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/elsa/otlp/v1/traces")
        {
            Content = CreateEmptyProtobufContent()
        };
        request.Headers.Add("x-test-remote-ip", "10.0.0.5");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostTraces_WhenApiKeyMatches_AllowsNonLoopbackIngestion()
    {
        await using var app = await CreateAppAsync(options => options.ApiKey = "secret");
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/elsa/otlp/v1/traces")
        {
            Content = CreateEmptyProtobufContent()
        };
        request.Headers.Add("x-test-remote-ip", "10.0.0.5");
        request.Headers.Add("x-otlp-api-key", "secret");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CollectorConfiguration_WhenApiKeyIsConfigured_DoesNotExposeSecret()
    {
        var provider = new CollectorConfigurationProvider(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions { ApiKey = "secret" }));

        var configuration = await provider.GetAsync();

        Assert.Equal("<configured>", configuration.RequiredHeaders["x-otlp-api-key"]);
        Assert.DoesNotContain("secret", configuration.RequiredHeaders.Values);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<OpenTelemetryDiagnosticsOptions>? configure = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenTelemetryDiagnosticsServices(options =>
        {
            options.AllowUnauthenticatedLoopback = true;
            options.HttpEndpointPath = "/elsa/otlp/v1";
            configure?.Invoke(options);
        });

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("x-test-remote-ip", out var value) && IPAddress.TryParse(value.ToString(), out var remoteIpAddress))
                context.Connection.RemoteIpAddress = remoteIpAddress;

            await next();
        });
        app.MapOpenTelemetryHttpProtobufCollector();
        await app.StartAsync();
        return app;
    }

    private static ByteArrayContent CreateEmptyProtobufContent()
    {
        var content = new ByteArrayContent([]);
        content.Headers.ContentType = new("application/x-protobuf");
        return content;
    }
}

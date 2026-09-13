using System.Net;
using Elsa.Diagnostics.OpenTelemetry.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace Elsa.Diagnostics.OpenTelemetry.IntegrationTests;

/// <summary>
/// Marker type whose assembly supplies the dependency context for the in-process OTLP collector host.
/// </summary>
public sealed class OpenTelemetryTestEntryPoint;

/// <summary>
/// Defines a stateless host recipe. TUnit.AspNetCore derives and owns a separate host for every test.
/// </summary>
public sealed class OpenTelemetryWebApplicationFactory : TestWebApplicationFactory<OpenTelemetryTestEntryPoint>
{
    public const string TestRemoteIpAddressHeaderName = "x-test-remote-ip";

    // Starting from a bare host avoids reload-on-change configuration sources and their file watchers.
    protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddOpenTelemetryDiagnosticsServices(options =>
                {
                    options.AllowUnauthenticatedLoopback = true;
                    options.HttpEndpointPath = "/elsa/otlp/v1";
                });
            })
            .Configure(app =>
            {
                app.Use(async (context, next) =>
                {
                    if (context.Request.Headers.TryGetValue(TestRemoteIpAddressHeaderName, out var value) &&
                        IPAddress.TryParse(value.ToString(), out var remoteIpAddress))
                        context.Connection.RemoteIpAddress = remoteIpAddress;
                    else
                        context.Connection.RemoteIpAddress = IPAddress.Loopback;

                    await next();
                });
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapOpenTelemetryHttpProtobufCollector());
            });
    }
}

public abstract class OpenTelemetryWebApplicationTest : WebApplicationTest<OpenTelemetryWebApplicationFactory, OpenTelemetryTestEntryPoint>
{
    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        // These tests exercise Elsa's OpenTelemetry collector. The harness must not add its own
        // OpenTelemetry processor/instrumentation or modify IHttpClientFactory pipelines in the SUT.
        options.AutoConfigureOpenTelemetry = false;
        options.AutoPropagateHttpClientFactory = false;
    }
}

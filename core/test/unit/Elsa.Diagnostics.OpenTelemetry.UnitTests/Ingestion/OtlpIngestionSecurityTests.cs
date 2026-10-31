using System.Net;
using Elsa.Diagnostics.OpenTelemetry.Ingestion;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.AspNetCore.Http;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Ingestion;

public class OtlpIngestionSecurityTests
{
    [Fact(DisplayName = "Loopback ingestion is allowed without API key in development mode")]
    public void AllowsLoopbackWithoutApiKey()
    {
        var context = CreateContext(IPAddress.Loopback);

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions());

        Assert.True(authorized);
    }

    [Fact(DisplayName = "Non-loopback ingestion is rejected without API key")]
    public void RejectsNonLoopbackWithoutApiKey()
    {
        var context = CreateContext(IPAddress.Parse("10.0.0.5"));

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions());

        Assert.False(authorized);
    }

    [Fact(DisplayName = "Missing remote address is rejected without API key")]
    public void RejectsMissingRemoteAddressWithoutApiKey()
    {
        var context = CreateContext(null);

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions());

        Assert.False(authorized);
    }

    [Fact(DisplayName = "Configured API key header authorizes ingestion")]
    public void AllowsConfiguredApiKey()
    {
        var context = CreateContext(IPAddress.Parse("10.0.0.5"));
        context.Request.Headers["x-otlp-api-key"] = "secret";

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions { ApiKey = "secret" });

        Assert.True(authorized);
    }

    [Fact(DisplayName = "Incorrect API key header rejects ingestion")]
    public void RejectsIncorrectConfiguredApiKey()
    {
        var context = CreateContext(IPAddress.Parse("10.0.0.5"));
        context.Request.Headers["x-otlp-api-key"] = "not-secret";

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions { ApiKey = "secret" });

        Assert.False(authorized);
    }

    private static DefaultHttpContext CreateContext(IPAddress? remoteAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteAddress;
        return context;
    }
}

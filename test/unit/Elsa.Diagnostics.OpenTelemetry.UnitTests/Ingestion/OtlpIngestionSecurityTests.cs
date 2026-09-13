using System.Net;
using Elsa.Diagnostics.OpenTelemetry.Ingestion;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Ingestion;

public class OtlpIngestionSecurityTests
{
    [Test]
    [DisplayName("Loopback ingestion is allowed without API key in development mode")]
    public async Task AllowsLoopbackWithoutApiKey()
    {
        var context = CreateContext(IPAddress.Loopback);

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions());

        await Assert.That(authorized).IsTrue();
    }

    [Test]
    [DisplayName("Non-loopback ingestion is rejected without API key")]
    public async Task RejectsNonLoopbackWithoutApiKey()
    {
        var context = CreateContext(IPAddress.Parse("10.0.0.5"));

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions());

        await Assert.That(authorized).IsFalse();
    }

    [Test]
    [DisplayName("Missing remote address is rejected without API key")]
    public async Task RejectsMissingRemoteAddressWithoutApiKey()
    {
        var context = CreateContext(null);

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions());

        await Assert.That(authorized).IsFalse();
    }

    [Test]
    [DisplayName("Configured API key header authorizes ingestion")]
    public async Task AllowsConfiguredApiKey()
    {
        var context = CreateContext(IPAddress.Parse("10.0.0.5"));
        context.Request.Headers["x-otlp-api-key"] = "secret";

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions { ApiKey = "secret" });

        await Assert.That(authorized).IsTrue();
    }

    [Test]
    [DisplayName("Incorrect API key header rejects ingestion")]
    public async Task RejectsIncorrectConfiguredApiKey()
    {
        var context = CreateContext(IPAddress.Parse("10.0.0.5"));
        context.Request.Headers["x-otlp-api-key"] = "not-secret";

        var authorized = OtlpIngestionSecurity.IsAuthorized(context, new OpenTelemetryDiagnosticsOptions { ApiKey = "secret" });

        await Assert.That(authorized).IsFalse();
    }

    private static DefaultHttpContext CreateContext(IPAddress? remoteAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteAddress;
        return context;
    }
}
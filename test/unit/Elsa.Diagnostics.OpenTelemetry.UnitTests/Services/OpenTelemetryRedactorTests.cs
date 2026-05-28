using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Services;

public class OpenTelemetryRedactorTests
{
    [Fact]
    public void Redact_WhenSensitiveAttributeNameExists_RedactsValue()
    {
        var redactor = new OpenTelemetryRedactor(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions()));
        var log = new OtlpLogRecord("1", "resource", DateTimeOffset.UtcNow, "Information", null, "hello", null, null, new Dictionary<string, string?> { ["password"] = "secret" });
        var batch = new OpenTelemetryBatch([], [], [], [], [], [log]);

        var result = redactor.Redact(batch);

        Assert.Equal("[Redacted]", result.Logs.Single().Attributes["password"]);
    }
}

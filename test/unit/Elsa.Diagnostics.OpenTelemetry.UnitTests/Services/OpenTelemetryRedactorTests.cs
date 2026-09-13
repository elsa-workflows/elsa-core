using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Services;

public class OpenTelemetryRedactorTests
{
    [Test]
    public async Task Redact_WhenSensitiveAttributeNameExists_RedactsValue()
    {
        var redactor = new OpenTelemetryRedactor(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions()));
        var log = new OtlpLogRecord("1", "resource", DateTimeOffset.UtcNow, "Information", null, "hello", null, null, new Dictionary<string, string?> { ["password"] = "secret" });
        var batch = new OpenTelemetryBatch([], [], [], [], [], [log]);

        var result = redactor.Redact(batch);

        await Assert.That(result.Logs.Single().Attributes["password"]).IsEqualTo("[Redacted]");
    }

    [Test]
    public async Task Redact_WhenAttributeKeysDifferOnlyByCase_UsesLastValue()
    {
        var redactor = new OpenTelemetryRedactor(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions()));
        var log = new OtlpLogRecord("1", "resource", DateTimeOffset.UtcNow, "Information", null, "hello", null, null, new Dictionary<string, string?>
        {
            ["Password"] = "first",
            ["password"] = "second"
        });
        var batch = new OpenTelemetryBatch([], [], [], [], [], [log]);

        var result = redactor.Redact(batch);

        var attributes = result.Logs.Single().Attributes;
        await Assert.That(attributes).HasSingleItem();
        await Assert.That(attributes["Password"]).IsEqualTo("[Redacted]");
    }
}
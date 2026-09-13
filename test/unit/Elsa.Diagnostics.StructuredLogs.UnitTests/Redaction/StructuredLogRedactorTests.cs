using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.Redaction;

public class StructuredLogRedactorTests
{
    private readonly StructuredLogRedactor _redactor = new(MicrosoftOptions.Create(new StructuredLogsOptions()));

    [Test]
    public async Task Redact_WhenPropertyNameIsSensitive_RedactsValue()
    {
        var redacted = _redactor.Redact(CreateLog() with
        {
            Properties = new Dictionary<string, string?>
            {
                ["AccessToken"] = "secret-token"
            }
        });

        await Assert.That(redacted.Properties["AccessToken"]).IsEqualTo("[Redacted]");
    }

    [Test]
    public async Task Redact_WhenMessageContainsSensitiveText_RedactsMatch()
    {
        var redacted = _redactor.Redact(CreateLog() with
        {
            Message = "Authorization: Bearer abc.def.ghi"
        });

        await Assert.That(redacted.Message).DoesNotContain("abc.def.ghi");
        await Assert.That(redacted.Message).Contains("[Redacted]");
    }

    [Test]
    public async Task Redact_WhenExceptionContainsSensitiveText_RedactsException()
    {
        var redacted = _redactor.Redact(CreateLog() with
        {
            Exception = new("System.Exception", "password=letmein", null)
        });

        await Assert.That(redacted.Exception!.Message).IsEqualTo("[Redacted]");
    }

    private static StructuredLogEvent CreateLog() =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            Level = StructuredLogLevel.Information,
            Category = "Elsa",
            Message = "Hello",
            SourceId = "source-a"
        };
}

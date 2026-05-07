using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Options;
using Elsa.Diagnostics.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.UnitTests.Redaction;

public class ServerLogRedactorTests
{
    private readonly ServerLogRedactor _redactor = new(MicrosoftOptions.Create(new ServerLogStreamingOptions()));

    [Fact]
    public void Redact_WhenPropertyNameIsSensitive_RedactsValue()
    {
        var redacted = _redactor.Redact(CreateLog() with
        {
            Properties = new Dictionary<string, string?>
            {
                ["AccessToken"] = "secret-token"
            }
        });

        Assert.Equal("[Redacted]", redacted.Properties["AccessToken"]);
    }

    [Fact]
    public void Redact_WhenMessageContainsSensitiveText_RedactsMatch()
    {
        var redacted = _redactor.Redact(CreateLog() with
        {
            Message = "Authorization: Bearer abc.def.ghi"
        });

        Assert.DoesNotContain("abc.def.ghi", redacted.Message);
        Assert.Contains("[Redacted]", redacted.Message);
    }

    [Fact]
    public void Redact_WhenExceptionContainsSensitiveText_RedactsException()
    {
        var redacted = _redactor.Redact(CreateLog() with
        {
            Exception = new("System.Exception", "password=letmein", null)
        });

        Assert.Equal("[Redacted]", redacted.Exception!.Message);
    }

    private static ServerLogEvent CreateLog() =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            Level = ServerLogLevel.Information,
            Category = "Elsa",
            Message = "Hello",
            SourceId = "source-a"
        };
}

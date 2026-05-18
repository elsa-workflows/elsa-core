using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Redaction;

public class ConsoleLogRedactorTests
{
    private readonly ConsoleLogRedactor _redactor = new(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()));

    [Fact]
    public void Redact_MasksSensitiveLineText()
    {
        var line = CreateLine(string.Concat("Authorization: ", "Bearer ", "sample-token"));

        var redacted = _redactor.Redact(line);

        Assert.Equal("Authorization: [Redacted]", redacted.Text);
    }

    [Fact]
    public void Redact_MasksSensitiveSourceMetadata()
    {
        var line = CreateLine("hello") with
        {
            Source = CreateSource() with
            {
                Metadata = new Dictionary<string, string?> { ["apiKey"] = "secret-value" }
            }
        };

        var redacted = _redactor.Redact(line);

        Assert.Equal("[Redacted]", redacted.Source.Metadata["apiKey"]);
    }

    private static ConsoleLogLine CreateLine(string text) => new()
    {
        Text = text,
        Source = CreateSource()
    };

    private static ConsoleLogSource CreateSource() => new()
    {
        Id = "source",
        DisplayName = "source",
        MachineName = "machine"
    };
}

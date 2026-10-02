using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogExportFormatterTests
{
    private readonly ConsoleLogExportFormatter _formatter = new();

    [Fact]
    public void FormatVisibleRows_IncludesTimestampStreamSourceAndRawText()
    {
        var timestamp = DateTimeOffset.Parse("2026-05-18T09:00:00Z");
        var line = new ConsoleLogLine
        {
            Timestamp = timestamp,
            Stream = ConsoleLogStream.Stderr,
            Source = new() { Id = "source-a", DisplayName = "Worker A" },
            Text = "raw\tconsole text"
        };

        var text = _formatter.FormatVisibleRows([line]);

        Assert.Contains(timestamp.ToString("O"), text);
        Assert.Contains("stderr", text);
        Assert.Contains("Worker A", text);
        Assert.Contains("raw\tconsole text", text);
    }

    [Fact]
    public void SourceDisplayName_UsesMetadataFallback()
    {
        var source = new ConsoleLogSource { Id = "source-a", ServiceName = "api", PodName = "pod-a" };

        var label = ConsoleLogExportFormatter.SourceDisplayName(source);

        Assert.Equal("api/pod-a/source-a", label);
    }
}

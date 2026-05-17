using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Filtering;

public class ConsoleLogFilterTests
{
    private readonly ConsoleLogLine _line = new()
    {
        Text = "workflow started",
        Stream = ConsoleLogStream.Stdout,
        Source = new ConsoleLogSource { Id = "source-a", DisplayName = "Source A", MachineName = "machine" },
        ReceivedAt = DateTimeOffset.Parse("2026-05-18T10:00:00Z")
    };

    [Fact]
    public void Matches_FiltersBySource()
    {
        Assert.True(ConsoleLogFilterEvaluator.Matches(_line, new ConsoleLogFilter { SourceId = "source-a" }));
        Assert.False(ConsoleLogFilterEvaluator.Matches(_line, new ConsoleLogFilter { SourceId = "source-b" }));
    }

    [Fact]
    public void Matches_FiltersByStream()
    {
        Assert.True(ConsoleLogFilterEvaluator.Matches(_line, new ConsoleLogFilter { Stream = ConsoleLogStream.Stdout }));
        Assert.False(ConsoleLogFilterEvaluator.Matches(_line, new ConsoleLogFilter { Stream = ConsoleLogStream.Stderr }));
    }

    [Fact]
    public void Matches_FiltersByTextAndTime()
    {
        Assert.True(ConsoleLogFilterEvaluator.Matches(_line, new ConsoleLogFilter { Query = "started", From = _line.ReceivedAt.AddSeconds(-1), To = _line.ReceivedAt.AddSeconds(1) }));
        Assert.False(ConsoleLogFilterEvaluator.Matches(_line, new ConsoleLogFilter { Query = "missing" }));
        Assert.False(ConsoleLogFilterEvaluator.Matches(_line, new ConsoleLogFilter { From = _line.ReceivedAt.AddSeconds(1) }));
    }
}

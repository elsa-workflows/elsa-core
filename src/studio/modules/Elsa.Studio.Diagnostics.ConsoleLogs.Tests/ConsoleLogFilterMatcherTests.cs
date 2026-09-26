using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogFilterMatcherTests
{
    [Fact]
    public void IsMatch_RejectsLineFromDifferentStream()
    {
        var line = Line(ConsoleLogStream.Stdout);
        var filter = new ConsoleLogFilter { Stream = ConsoleLogStream.Stderr };

        Assert.False(ConsoleLogFilterMatcher.IsMatch(line, filter));
    }

    [Fact]
    public void IsMatch_AcceptsLineFromSelectedStream()
    {
        var line = Line(ConsoleLogStream.Stderr);
        var filter = new ConsoleLogFilter { Stream = ConsoleLogStream.Stderr };

        Assert.True(ConsoleLogFilterMatcher.IsMatch(line, filter));
    }

    [Fact]
    public void IsMatch_RejectsLineFromDifferentSource()
    {
        var line = Line(ConsoleLogStream.Stderr);
        var filter = new ConsoleLogFilter { SourceId = "source-b" };

        Assert.False(ConsoleLogFilterMatcher.IsMatch(line, filter));
    }

    [Fact]
    public void IsMatch_RejectsLineOutsideTimeRange()
    {
        var line = Line(ConsoleLogStream.Stderr);
        line.ReceivedAt = DateTimeOffset.Parse("2026-05-18T09:00:00Z");
        var filter = new ConsoleLogFilter { From = DateTimeOffset.Parse("2026-05-18T10:00:00Z") };

        Assert.False(ConsoleLogFilterMatcher.IsMatch(line, filter));
    }

    [Fact]
    public void IsMatch_RejectsLineWithoutMatchingWorkflowMetadata()
    {
        var line = Line(ConsoleLogStream.Stderr);
        var filter = new ConsoleLogFilter { WorkflowInstanceId = "workflow-b" };

        Assert.False(ConsoleLogFilterMatcher.IsMatch(line, filter));
    }

    [Fact]
    public void IsMatch_AcceptsLineWithMatchingWorkflowMetadata()
    {
        var line = Line(
            ConsoleLogStream.Stderr,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["elsa.workflowInstanceId"] = "workflow-a"
            });
        var filter = new ConsoleLogFilter { WorkflowInstanceId = "workflow-a" };

        Assert.True(ConsoleLogFilterMatcher.IsMatch(line, filter));
    }

    private static ConsoleLogLine Line(ConsoleLogStream stream, IReadOnlyDictionary<string, string>? metadata = null) => new()
    {
        Id = Guid.NewGuid().ToString("n"),
        Timestamp = DateTimeOffset.Parse("2026-05-18T09:00:00Z"),
        ReceivedAt = DateTimeOffset.Parse("2026-05-18T09:00:01Z"),
        Stream = stream,
        Text = "resolved shell",
        Source = new() { Id = "source-a", DisplayName = "Source A" },
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    };
}

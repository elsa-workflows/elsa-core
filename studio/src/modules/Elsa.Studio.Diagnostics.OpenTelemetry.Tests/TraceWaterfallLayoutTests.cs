using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class TraceWaterfallLayoutTests
{
    private readonly DateTimeOffset _start = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WhenSpansHaveParentChain_AssignsNestedDepths()
    {
        var root = Span("root", null, 0, 100);
        var child = Span("child", "root", 10, 40);
        var grandchild = Span("grandchild", "child", 15, 20);

        var layout = TraceWaterfallLayout.Create([grandchild, child, root]).ToDictionary(x => x.Span.SpanId);

        Assert.Equal(0, layout["root"].Depth);
        Assert.Equal(1, layout["child"].Depth);
        Assert.Equal(2, layout["grandchild"].Depth);
    }

    [Fact]
    public void Create_WhenSpansAreUnordered_ReturnsStartTimeOrderWithRelativePosition()
    {
        var later = Span("later", null, 50, 100);
        var earlier = Span("earlier", null, 0, 25);

        var layout = TraceWaterfallLayout.Create([later, earlier]).ToArray();

        Assert.Equal("earlier", layout[0].Span.SpanId);
        Assert.Equal("later", layout[1].Span.SpanId);
        Assert.Equal(0, layout[0].OffsetPercent);
        Assert.Equal(50, layout[1].OffsetPercent);
        Assert.Equal(25, layout[0].WidthPercent);
        Assert.Equal(50, layout[1].WidthPercent);
    }

    [Fact]
    public void Create_WhenSpanHasZeroDuration_UsesMinimumVisibleWidth()
    {
        var layout = TraceWaterfallLayout.Create([Span("instant", null, 0, 0)]).Single();

        Assert.True(layout.WidthPercent > 0);
    }

    private TelemetrySpan Span(string spanId, string? parentSpanId, int startMilliseconds, int endMilliseconds)
    {
        return new(
            spanId,
            "trace-1",
            spanId,
            parentSpanId,
            "resource-1",
            spanId,
            "internal",
            _start.AddMilliseconds(startMilliseconds),
            _start.AddMilliseconds(endMilliseconds),
            SpanStatus.Ok,
            null,
            new Dictionary<string, string?>(),
            [],
            []);
    }
}

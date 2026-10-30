using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

public record TraceWaterfallItem(
    TelemetrySpan Span,
    int Depth,
    double OffsetPercent,
    double WidthPercent);

public static class TraceWaterfallLayout
{
    private const double MinimumWidthPercent = 0.25;

    public static IReadOnlyCollection<TraceWaterfallItem> Create(IReadOnlyCollection<TelemetrySpan> spans)
    {
        if (spans.Count == 0)
            return [];

        var ordered = spans.OrderBy(x => x.StartTime).ThenBy(x => x.EndTime).ThenBy(x => x.SpanId).ToArray();
        var start = ordered.Min(x => x.StartTime);
        var end = ordered.Max(x => x.EndTime);
        var totalMilliseconds = Math.Max(1, (end - start).TotalMilliseconds);
        var spanById = ordered.ToDictionary(x => x.SpanId, StringComparer.OrdinalIgnoreCase);
        var depthBySpanId = ordered.ToDictionary(x => x.SpanId, x => GetDepth(x, spanById), StringComparer.OrdinalIgnoreCase);

        return ordered
            .Select(span =>
            {
                var offset = ((span.StartTime - start).TotalMilliseconds / totalMilliseconds) * 100;
                var width = Math.Max(MinimumWidthPercent, ((span.EndTime - span.StartTime).TotalMilliseconds / totalMilliseconds) * 100);
                return new TraceWaterfallItem(span, depthBySpanId[span.SpanId], Clamp(offset), Clamp(width));
            })
            .ToArray();
    }

    private static int GetDepth(TelemetrySpan span, IReadOnlyDictionary<string, TelemetrySpan> spans)
    {
        var depth = 0;
        var current = span;

        while (!string.IsNullOrWhiteSpace(current.ParentSpanId))
        {
            if (!spans.TryGetValue(current.ParentSpanId, out var parent))
                break;

            depth++;
            current = parent;
        }

        return depth;
    }

    private static double Clamp(double value) => Math.Min(100, Math.Max(0, value));
}

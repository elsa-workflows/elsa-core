using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

public record MetricSeriesRow(
    MetricInstrument Instrument,
    int PointCount,
    DateTimeOffset? LastTimestamp,
    double? LastValue,
    double? LastSum,
    long? LastCount);

public static class MetricSeriesMapper
{
    public static IReadOnlyCollection<MetricSeriesRow> CreateRows(OpenTelemetryMetricResult metrics)
    {
        var pointsByInstrument = metrics.Points
            .GroupBy(x => x.InstrumentId)
            .ToDictionary(x => x.Key, x => x.OrderBy(p => p.Timestamp).ToArray(), StringComparer.OrdinalIgnoreCase);

        return metrics.Instruments
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(instrument =>
            {
                pointsByInstrument.TryGetValue(instrument.Id, out var points);
                var lastPoint = points?.LastOrDefault();

                return new MetricSeriesRow(
                    instrument,
                    points?.Length ?? 0,
                    lastPoint?.Timestamp,
                    lastPoint?.Value,
                    lastPoint?.Sum,
                    lastPoint?.Count);
            })
            .ToArray();
    }
}

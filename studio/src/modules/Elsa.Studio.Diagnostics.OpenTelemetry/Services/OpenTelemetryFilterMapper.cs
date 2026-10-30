using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

/// <summary>
/// Applies view-level row caps to OpenTelemetry query filters.
/// </summary>
public static class OpenTelemetryFilterMapper
{
    public static OpenTelemetryTraceFilter ToTraceRequest(OpenTelemetryTraceFilter filter, int rowCap)
    {
        return filter with { Take = ClampTake(filter.Take, rowCap) };
    }

    public static OpenTelemetryMetricFilter ToMetricRequest(OpenTelemetryMetricFilter filter, int rowCap)
    {
        return filter with { Take = ClampTake(filter.Take, rowCap) };
    }

    public static OpenTelemetryLogFilter ToLogRequest(OpenTelemetryLogFilter filter, int rowCap)
    {
        return filter with { Take = ClampTake(filter.Take, rowCap) };
    }

    private static int ClampTake(int? requested, int rowCap)
    {
        var effectiveCap = Math.Max(1, rowCap);
        return Math.Clamp(requested ?? effectiveCap, 1, effectiveCap);
    }
}

using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

public record OpenTelemetryUrlState
{
    public string? Tab { get; init; }
    public string? ResourceId { get; init; }
    public string? ServiceName { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? Severity { get; init; }
    public SpanStatus? Status { get; init; }
    public string? Text { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public bool? Live { get; init; }
}

public static class OpenTelemetryUrlStateMapper
{
    public static IReadOnlyDictionary<string, string> ToQuery(OpenTelemetryUrlState state)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(values, "tab", state.Tab);
        Add(values, "resource", state.ResourceId);
        Add(values, "service", state.ServiceName);
        Add(values, "trace", state.TraceId);
        Add(values, "span", state.SpanId);
        Add(values, "workflow", state.WorkflowInstanceId);
        Add(values, "definition", state.WorkflowDefinitionId);
        Add(values, "severity", state.Severity);
        Add(values, "status", state.Status?.ToString());
        Add(values, "text", state.Text);
        Add(values, "from", state.From?.ToString("O"));
        Add(values, "to", state.To?.ToString("O"));
        Add(values, "live", state.Live?.ToString().ToLowerInvariant());
        return values;
    }

    public static OpenTelemetryUrlState FromQuery(IReadOnlyDictionary<string, string?> query)
    {
        return new OpenTelemetryUrlState
        {
            Tab = Get(query, "tab"),
            ResourceId = Get(query, "resource"),
            ServiceName = Get(query, "service"),
            TraceId = Get(query, "trace"),
            SpanId = Get(query, "span"),
            WorkflowInstanceId = Get(query, "workflow"),
            WorkflowDefinitionId = Get(query, "definition"),
            Severity = Get(query, "severity"),
            Status = Enum.TryParse<SpanStatus>(Get(query, "status"), ignoreCase: true, out var status) ? status : null,
            Text = Get(query, "text"),
            From = DateTimeOffset.TryParse(Get(query, "from"), out var from) ? from : null,
            To = DateTimeOffset.TryParse(Get(query, "to"), out var to) ? to : null,
            Live = bool.TryParse(Get(query, "live"), out var live) ? live : null
        };
    }

    public static OpenTelemetryTraceFilter ToTraceFilter(OpenTelemetryUrlState state, int rowCap)
    {
        return OpenTelemetryFilterMapper.ToTraceRequest(new OpenTelemetryTraceFilter
        {
            ResourceId = state.ResourceId,
            ServiceName = state.ServiceName,
            TraceId = state.TraceId,
            WorkflowInstanceId = state.WorkflowInstanceId,
            Status = state.Status,
            From = state.From,
            To = state.To,
            Search = state.Text
        }, rowCap);
    }

    public static OpenTelemetryLogFilter ToLogFilter(OpenTelemetryUrlState state, int rowCap)
    {
        return OpenTelemetryFilterMapper.ToLogRequest(new OpenTelemetryLogFilter
        {
            ResourceId = state.ResourceId,
            ServiceName = state.ServiceName,
            TraceId = state.TraceId,
            SpanId = state.SpanId,
            Severity = state.Severity,
            From = state.From,
            To = state.To,
            Search = state.Text
        }, rowCap);
    }

    public static OpenTelemetryMetricFilter ToMetricFilter(OpenTelemetryUrlState state, int rowCap)
    {
        return OpenTelemetryFilterMapper.ToMetricRequest(new OpenTelemetryMetricFilter
        {
            ResourceId = state.ResourceId,
            ServiceName = state.ServiceName,
            InstrumentName = state.Text,
            From = state.From,
            To = state.To
        }, rowCap);
    }

    private static void Add(IDictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            values[key] = value;
    }

    private static string? Get(IReadOnlyDictionary<string, string?> query, string key)
    {
        return query.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }
}

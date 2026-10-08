using Elsa.Studio.Diagnostics.StructuredLogs.Models;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Services;

/// <summary>
/// Maps UI filter state to backend request filters.
/// </summary>
public static class StructuredLogFilterMapper
{
    /// <summary>
    /// Creates a filter for recent-log requests and clamps the requested row count to the UI cap.
    /// </summary>
    public static StructuredLogFilter ToRecentRequest(StructuredLogFilter filter, int rowCap)
    {
        var request = Copy(filter);
        request.Take = Math.Clamp(request.Take ?? rowCap, 1, rowCap);
        return request;
    }

    /// <summary>
    /// Creates a filter for live SignalR subscriptions.
    /// </summary>
    public static StructuredLogFilter ToLiveSubscription(StructuredLogFilter filter) => Copy(filter);

    private static StructuredLogFilter Copy(StructuredLogFilter filter) =>
        new()
        {
            MinimumLevel = filter.MinimumLevel,
            Levels = filter.Levels?.ToList(),
            CategoryPrefix = filter.CategoryPrefix,
            Text = filter.Text,
            TenantId = filter.TenantId,
            WorkflowDefinitionId = filter.WorkflowDefinitionId,
            WorkflowInstanceId = filter.WorkflowInstanceId,
            TraceId = filter.TraceId,
            SpanId = filter.SpanId,
            CorrelationId = filter.CorrelationId,
            SourceId = filter.SourceId,
            From = filter.From,
            To = filter.To,
            Take = filter.Take
        };
}

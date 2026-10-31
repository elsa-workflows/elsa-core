using Elsa.Studio.Diagnostics.ConsoleLogs.Models;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Maps UI filter state to backend request filters.
/// </summary>
public static class ConsoleLogFilterMapper
{
    /// <summary>
    /// Creates a filter for recent-line requests and clamps the requested row count to the UI cap.
    /// </summary>
    public static ConsoleLogFilter ToRecentRequest(ConsoleLogFilter filter, int rowCap)
    {
        var request = Copy(filter);
        request.Limit = Math.Clamp(request.Limit ?? rowCap, 1, rowCap);
        return request;
    }

    /// <summary>
    /// Creates a filter for live SignalR subscriptions.
    /// </summary>
    public static ConsoleLogFilter ToLiveSubscription(ConsoleLogFilter filter) => Copy(filter);

    private static ConsoleLogFilter Copy(ConsoleLogFilter filter) =>
        new()
        {
            SourceId = filter.SourceId,
            Stream = filter.Stream,
            Query = filter.Query,
            WorkflowInstanceId = filter.WorkflowInstanceId,
            ActivityInstanceId = filter.ActivityInstanceId,
            ActivityId = filter.ActivityId,
            ActivityNodeId = filter.ActivityNodeId,
            From = filter.From,
            To = filter.To,
            Limit = filter.Limit
        };
}

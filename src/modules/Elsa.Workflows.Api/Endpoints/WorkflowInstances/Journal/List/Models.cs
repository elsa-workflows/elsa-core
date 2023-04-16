using FastEndpoints;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.List;

/// <summary>
/// Represents a request for a page of workflow execution log records.
/// </summary>
public class Request
{
    /// <summary>
    /// The ID of the workflow instance to get the execution log for.
    /// </summary>
    [BindFrom("id")] public string WorkflowInstanceId { get; set; } = default!;
    
    /// <summary>
    /// The zero-based page number to get.
    /// </summary>
    public int? Page { get; set; }
    
    /// <summary>
    /// The size of the page to get.
    /// </summary>
    public int? PageSize { get; set; }
}

public class Response
{
    public Response(ICollection<ExecutionLogRecord> items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public ICollection<ExecutionLogRecord> Items { get; }
    public long TotalCount { get; }
}

public record ExecutionLogRecord(
    string Id,
    string ActivityInstanceId,
    string? ParentActivityInstanceId,
    string ActivityId,
    string ActivityType,
    string NodeId,
    DateTimeOffset Timestamp,
    string? EventName,
    string? Message,
    string? Source,
    IDictionary<string, object>? ActivityState,
    object? Payload);
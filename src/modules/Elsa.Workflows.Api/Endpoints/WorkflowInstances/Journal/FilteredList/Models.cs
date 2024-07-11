using Elsa.Workflows.Api.Models;
using FastEndpoints;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.FilteredList;

/// Represents a request for a page of workflow execution log records.
internal class Request
{
    /// The ID of the workflow instance to get the execution log for.
    [BindFrom("id")] public string WorkflowInstanceId { get; set; } = default!;
    
    /// The filter to apply.
    public JournalFilter? Filter { get; set; }
    
    /// The zero-based page number to get.
    public int? Page { get; set; }
    
    /// The size of the page to get.
    public int? PageSize { get; set; }
    
    /// The number of records to skip.
    public int? Skip { get; set; }
    
    /// The number of records to take.
    public int? Take { get; set; }
}

internal class JournalFilter
{
    public ICollection<string>? ActivityIds { get; set; }
    public ICollection<string>? ActivityNodeIds { get; set; }
    public ICollection<string>? ExcludedActivityTypes { get; set; }
    public ICollection<string>? EventNames { get; set; }
}

internal class Response(ICollection<ExecutionLogRecord> items, long totalCount)
{
    public ICollection<ExecutionLogRecord> Items { get; } = items;
    public long TotalCount { get; } = totalCount;
}
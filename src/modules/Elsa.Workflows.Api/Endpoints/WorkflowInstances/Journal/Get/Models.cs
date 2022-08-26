using System;
using System.Collections.Generic;
using FastEndpoints;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.Get;

public class Request
{
    [BindFrom("id")] public string WorkflowInstanceId { get; set; } = default!;
    public int? Page { get; set; }
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
    DateTimeOffset Timestamp,
    string? EventName,
    string? Message,
    string? Source,
    object? Payload);
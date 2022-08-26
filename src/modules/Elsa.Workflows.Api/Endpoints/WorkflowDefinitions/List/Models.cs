using System.Collections.Generic;
using Elsa.Workflows.Persistence.Models;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;

public class Request
{
    public string? VersionOptions { get; set; }
    public string[]? DefinitionIds { get; set; }
    [BindFrom("materializer")] public string? MaterializerName { get; set; }
    [BindFrom("label")] public string[]? Labels { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public class Response
{
    public Response(ICollection<WorkflowDefinitionSummary> items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public ICollection<WorkflowDefinitionSummary> Items { get; set; }
    public long TotalCount { get; set; }
}
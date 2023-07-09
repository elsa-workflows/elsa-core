using Elsa.Common.Entities;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.List;

public class Request
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public string? DefinitionId { get; set; }
    public ICollection<string>? DefinitionIds { get; set; }
    public string? CorrelationId { get; set; }
    public int? Version { get; set; }
    public WorkflowStatus? Status { get; set; }
    public ICollection<string>? Statuses { get; set; }
    public WorkflowSubStatus? SubStatus { get; set; }
    public ICollection<string>? SubStatuses { get; set; }
    public OrderByWorkflowInstance? OrderBy { get; set; }
    public OrderDirection? OrderDirection { get; set; }
}

public class Response
{
    public Response(ICollection<WorkflowInstanceSummary> items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public ICollection<WorkflowInstanceSummary> Items { get; set; }
    public long TotalCount { get; set; }
}
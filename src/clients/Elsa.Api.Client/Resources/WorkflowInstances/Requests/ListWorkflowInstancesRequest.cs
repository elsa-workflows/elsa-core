using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

public class ListWorkflowInstancesRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public string? DefinitionId { get; set; }
    [Query(CollectionFormat.Multi)] public ICollection<string>? DefinitionIds { get; set; }
    public string? CorrelationId { get; set; }
    public int? Version { get; set; }
    public WorkflowStatus? Status { get; set; }
    public WorkflowSubStatus? SubStatus { get; set; }
    public OrderByWorkflowInstance? OrderBy { get; set; }
    public OrderDirection? OrderDirection { get; set; }
}
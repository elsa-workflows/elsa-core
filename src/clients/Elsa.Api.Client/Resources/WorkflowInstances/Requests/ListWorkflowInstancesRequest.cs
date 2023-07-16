using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

/// <summary>
/// Represents a request to list workflow instances.
/// </summary>
public class ListWorkflowInstancesRequest
{
    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int? Page { get; set; }
    
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int? PageSize { get; set; }
    
    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Gets or sets the workflow definition id.
    /// </summary>
    public string? DefinitionId { get; set; }
    
    /// <summary>
    /// Gets or sets the workflow definition ids.
    /// </summary>
    [Query(CollectionFormat.Multi)] public ICollection<string>? DefinitionIds { get; set; }
    
    /// <summary>
    /// Gets or sets the correlation id.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    public int? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public WorkflowStatus? Status { get; set; }
    
    /// <summary>
    /// Gets or sets the statuses.
    /// </summary>
    [Query(CollectionFormat.Multi)] public ICollection<WorkflowStatus>? Statuses { get; set; }
    
    /// <summary>
    /// Gets or sets the sub status.
    /// </summary>
    public WorkflowSubStatus? SubStatus { get; set; }
    
    /// <summary>
    /// Gets or sets the sub statuses.
    /// </summary>
    [Query(CollectionFormat.Multi)] public ICollection<WorkflowSubStatus>? SubStatuses { get; set; }

    /// <summary>
    /// Gets or sets the key to order by.
    /// </summary>
    public OrderByWorkflowInstance? OrderBy { get; set; }
    
    /// <summary>
    /// Gets or sets the order direction.
    /// </summary>
    public OrderDirection? OrderDirection { get; set; }
}
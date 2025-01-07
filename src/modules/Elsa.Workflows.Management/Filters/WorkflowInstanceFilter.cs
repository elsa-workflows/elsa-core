using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Models;
using System.Linq.Dynamic.Core;

namespace Elsa.Workflows.Management.Filters;

/// <summary>
/// A filter for querying workflow instances.
/// </summary>
public class WorkflowInstanceFilter
{
    /// <summary>
    /// Filter workflow instances by ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Filter workflow instances by IDs.
    /// </summary>
    public ICollection<string>? Ids { get; set; }

    /// <summary>
    /// Filter workflow instances that match the specified search term.
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Filter workflow instances that match the specified name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter workflow instances by definition ID.
    /// </summary>
    public string? DefinitionId { get; set; }

    /// <summary>
    /// Filter workflow instances by definition version ID.
    /// </summary>
    public string? DefinitionVersionId { get; set; }

    /// <summary>
    /// Filter workflow instances by definition IDs.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }

    /// <summary>
    /// Filter workflow instances by definition version IDs.
    /// </summary>
    public ICollection<string>? DefinitionVersionIds { get; set; }

    /// <summary>
    /// Filter workflow instances by version.
    /// </summary>
    public int? Version { get; set; }
    
    /// <summary>
    /// Filter workflow instances by their parent instance IDs.
    /// </summary>
    public ICollection<string>? ParentWorkflowInstanceIds { get; set; }

    /// <summary>
    /// Filter workflow instances by correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Filter workflow instances by correlation IDs.
    /// </summary>
    public ICollection<string>? CorrelationIds { get; set; }

    /// <summary>
    /// Filter workflow instances by status.
    /// </summary>
    public WorkflowStatus? WorkflowStatus { get; set; }

    /// <summary>
    /// Filter workflow instances by a set of statuses.
    /// </summary>
    public ICollection<WorkflowStatus>? WorkflowStatuses { get; set; }

    /// <summary>
    /// Filter workflow instances by sub-status.
    /// </summary>
    public WorkflowSubStatus? WorkflowSubStatus { get; set; }

    /// <summary>
    /// Filter workflow instances by a set of sub-status.
    /// </summary>
    public ICollection<WorkflowSubStatus>? WorkflowSubStatuses { get; set; }

    /// <summary>
    /// Filter workflow instances by whether they have incidents.
    /// </summary>
    public bool? HasIncidents { get; set; }
    
    /// <summary>
    /// Filter on workflows that are system workflows.
    /// </summary>
    public bool? IsSystem { get; set; }

    /// <summary>
    /// Filter workflow instances by timestamp.
    /// </summary>
    public ICollection<TimestampFilter>? TimestampFilters { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    [RequiresUnreferencedCode("The method uses reflection to create an expression tree.")]
    public IQueryable<WorkflowInstance> Apply(IQueryable<WorkflowInstance> query)
    {
        var filter = this;
        
        if (!string.IsNullOrWhiteSpace(filter.Id)) query = query.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) query = query.Where(x => filter.Ids.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(filter.DefinitionId)) query = query.Where(x => x.DefinitionId == filter.DefinitionId);
        if (!string.IsNullOrWhiteSpace(filter.DefinitionVersionId)) query = query.Where(x => x.DefinitionVersionId == filter.DefinitionVersionId);
        if (filter.DefinitionIds != null) query = query.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.DefinitionVersionIds != null) query = query.Where(x => filter.DefinitionVersionIds.Contains(x.DefinitionVersionId));
        if (filter.Version != null) query = query.Where(x => x.Version == filter.Version);
        if (filter.ParentWorkflowInstanceIds != null) query = query.Where(x => x.ParentWorkflowInstanceId != null && filter.ParentWorkflowInstanceIds.Contains(x.ParentWorkflowInstanceId));
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId)) query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        if (filter.CorrelationIds != null) query = query.Where(x => filter.CorrelationIds.Contains(x.CorrelationId!));
        if (filter.WorkflowStatus != null) query = query.Where(x => x.Status == filter.WorkflowStatus);
        if (filter.WorkflowSubStatus != null) query = query.Where(x => x.SubStatus == filter.WorkflowSubStatus);
        if (filter.WorkflowStatuses != null) query = query.Where(x => filter.WorkflowStatuses.Contains(x.Status));
        if (filter.WorkflowSubStatuses != null) query = query.Where(x => filter.WorkflowSubStatuses.Contains(x.SubStatus));
        if (filter.HasIncidents != null) query = filter.HasIncidents == true ? query.Where(x => x.IncidentCount > 0) : query.Where(x => x.IncidentCount == 0);
        if (filter.IsSystem != null) query = query.Where(x => x.IsSystem == filter.IsSystem);
        if (filter.Name != null) query = query.Where(x => x.Name!.ToLower().Contains(filter.Name.ToLower()));

        if (TimestampFilters != null)
        {
            foreach (TimestampFilter timestampFilter in TimestampFilters)
            {
                var column = timestampFilter.Column;
                var timestamp = timestampFilter.Timestamp;
                var isZeroTime = timestamp.TimeOfDay == TimeSpan.Zero;
                var startDay = new DateTimeOffset(timestamp.Date);
                var endDay = startDay.AddDays(1);

                query = timestampFilter.Operator switch
                {
                    TimestampFilterOperator.Is when isZeroTime => query.Where($"{column} >= @0 && {column} < @1", startDay, endDay),
                    TimestampFilterOperator.Is => query.Where($"{column} == @0", timestamp),
                    TimestampFilterOperator.IsNot when isZeroTime => query.Where($"{column} < @0 || {column} >= @1", startDay, endDay),
                    TimestampFilterOperator.IsNot => query.Where($"{column} != @0", timestamp),
                    TimestampFilterOperator.GreaterThan when isZeroTime => query.Where($"{column} > @0", endDay),
                    TimestampFilterOperator.GreaterThan => query.Where($"{column} > @0", timestamp),
                    TimestampFilterOperator.GreaterThanOrEqual when isZeroTime => query.Where($"{column} >= @0", startDay),
                    TimestampFilterOperator.GreaterThanOrEqual => query.Where($"{column} >= @0", timestamp),
                    TimestampFilterOperator.LessThan when isZeroTime => query.Where($"{column} < @0", startDay),
                    TimestampFilterOperator.LessThan => query.Where($"{column} < @0", timestamp),
                    TimestampFilterOperator.LessThanOrEqual when isZeroTime => query.Where($"{column} <= @0", endDay),
                    TimestampFilterOperator.LessThanOrEqual => query.Where($"{column} <= @0", timestamp),
                    _ => query
                };
            }
        }

        var searchTerm = filter.SearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query =
                from instance in query
                where instance.Name!.ToLower().Contains(searchTerm.ToLower())
                      || instance.DefinitionVersionId.Contains(searchTerm)
                      || instance.DefinitionId.Contains(searchTerm)
                      || instance.Id.Contains(searchTerm)
                      || instance.CorrelationId!.Contains(searchTerm)
                select instance;
        }

        return query;
    }
}
using Elsa.Workflows;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Retention.Extensions;

public static class WorkflowInstanceFilterExtensions
{

    /// <summary>
    /// Clone the current filter
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static WorkflowInstanceFilter Clone(this WorkflowInstanceFilter filter)
    {
        return new WorkflowInstanceFilter
        {
            Id = filter.Id,
            Ids = filter.Ids == null ? null : new List<string>(filter.Ids),
            Version = filter.Version,
            CorrelationId = filter.CorrelationId,
            CorrelationIds = filter.CorrelationIds == null ? null : new List<string>(filter.CorrelationIds),
            DefinitionId = filter.DefinitionId,
            DefinitionIds = filter.DefinitionIds == null ? null : new List<string>(filter.DefinitionIds),
            HasIncidents = filter.HasIncidents,
            IsSystem = filter.IsSystem,
            SearchTerm = filter.SearchTerm,
            TimestampFilters = filter.TimestampFilters?.Select(x => new TimestampFilter
            {
                Column = x.Column,
                Operator = x.Operator,
                Timestamp = x.Timestamp
            }).ToList(),
            WorkflowStatus = filter.WorkflowStatus,
            WorkflowStatuses = filter.WorkflowStatuses == null ? null : new List<WorkflowStatus>(filter.WorkflowStatuses),
            DefinitionVersionId = filter.DefinitionVersionId,
            DefinitionVersionIds = filter.DefinitionVersionIds == null ? null : new List<string>(filter.DefinitionVersionIds),
            WorkflowSubStatus = filter.WorkflowSubStatus,
            WorkflowSubStatuses = filter.WorkflowSubStatuses == null ? null : new List<WorkflowSubStatus>(filter.WorkflowSubStatuses),
            ParentWorkflowInstanceIds =
                filter.ParentWorkflowInstanceIds == null ? null : new List<string>(filter.ParentWorkflowInstanceIds)
        };
    }
}
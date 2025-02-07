using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class WorkflowInstanceFinder(IWorkflowInstanceStore workflowInstanceStore, IActivityExecutionStore activityExecutionStore) : IWorkflowInstanceFinder
{
    /// <inheritdoc />
    public async Task<IEnumerable<string>> FindAsync(AlterationWorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowInstanceFilter = new WorkflowInstanceFilter
        {
            Ids = filter.WorkflowInstanceIds?.ToList(),
            DefinitionIds = filter.DefinitionIds,
            DefinitionVersionIds = filter.DefinitionVersionIds?.ToList(),
            CorrelationIds = filter.CorrelationIds?.ToList(),
            HasIncidents = filter.HasIncidents,
            IsSystem = filter.IsSystem,
            TimestampFilters = filter.TimestampFilters?.ToList(),
            WorkflowStatuses = filter.Statuses?.ToList(),
            WorkflowSubStatuses = filter.SubStatuses?.ToList(),
            Names = filter.Names?.ToList(),
            SearchTerm = filter.SearchTerm,
        };
        var activityExecutionFilters = filter.ActivityFilters?.Select(x => new ActivityExecutionRecordFilter
        {
            ActivityId = x.ActivityId,
            Id = x.ActivityInstanceId,
            ActivityNodeId = x.NodeId,
            Name = x.Name,
            Status = x.Status,
        }).ToList();

        var workflowInstanceFilterIsEmpty = WorkflowFilterIsEmpty(workflowInstanceFilter);

        var workflowInstanceIds = workflowInstanceFilterIsEmpty
            ? Enumerable.Empty<string>().ToHashSet()
            : (await workflowInstanceStore.FindManyIdsAsync(workflowInstanceFilter, cancellationToken)).ToHashSet();

        if (activityExecutionFilters == null)
            return workflowInstanceIds;

        foreach (ActivityExecutionRecordFilter activityExecutionFilter in activityExecutionFilters.Where(x => !x.IsEmpty))
        {
            var activityExecutionRecords = await activityExecutionStore.FindManySummariesAsync(activityExecutionFilter, cancellationToken);
            var matchingWorkflowInstanceIds = activityExecutionRecords.Select(x => x.WorkflowInstanceId).ToHashSet();

            if (workflowInstanceFilterIsEmpty)
                workflowInstanceIds = matchingWorkflowInstanceIds;
            else
                workflowInstanceIds.IntersectWith(matchingWorkflowInstanceIds);
        }
        
        // Alterations must apply only to running workflows.
        workflowInstanceIds = (await FilterRunningWorkflowInstancesAsync(workflowInstanceIds, cancellationToken)).ToHashSet();

        return workflowInstanceIds;
    }

    private async Task<IEnumerable<string>> FilterRunningWorkflowInstancesAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter
        {
            Ids = workflowInstanceIds.ToList(),
            WorkflowStatus = WorkflowStatus.Running
        };
        
        return await workflowInstanceStore.FindManyIdsAsync(filter, cancellationToken);
    }

    private bool WorkflowFilterIsEmpty(WorkflowInstanceFilter filter)
    {
        return filter.Id == null &&
               filter.Ids == null &&
               filter.DefinitionId == null &&
               filter.DefinitionVersionId == null &&
               filter.DefinitionIds == null &&
               filter.DefinitionVersionIds == null &&
               filter.Version == null &&
               filter.CorrelationId == null &&
               filter.CorrelationIds == null &&
               filter.HasIncidents == null &&
               filter.TimestampFilters == null
               && string.IsNullOrWhiteSpace(filter.SearchTerm);
    }
}
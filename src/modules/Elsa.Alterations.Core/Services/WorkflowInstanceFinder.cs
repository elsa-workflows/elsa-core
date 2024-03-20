using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
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
            DefinitionVersionIds = filter.DefinitionVersionIds?.ToList(),
            CorrelationIds = filter.CorrelationIds?.ToList(),
            HasIncidents = filter.HasIncidents,
            IsSystem = filter.IsSystem,
            TimestampFilters = filter.TimestampFilters?.ToList(),
        };
        var activityExecutionFilters = filter.ActivityFilters?.Select(x => new ActivityExecutionRecordFilter
        {
            ActivityId = x.ActivityId,
            Id = x.ActivityInstanceId,
            ActivityNodeId = x.NodeId,
            Name = x.Name,
            Status = x.Status,
        }).ToList();

        var workflowInstanceIds = workflowInstanceFilter.IsEmpty
            ? Enumerable.Empty<string>().ToHashSet()
            : (await workflowInstanceStore.FindManyIdsAsync(workflowInstanceFilter, cancellationToken)).ToHashSet();

        if (activityExecutionFilters == null)
            return workflowInstanceIds;

        foreach (ActivityExecutionRecordFilter activityExecutionFilter in activityExecutionFilters.Where(x => !x.IsEmpty))
        {
            var activityExecutionRecords = await activityExecutionStore.FindManySummariesAsync(activityExecutionFilter, cancellationToken);
            var matchingWorkflowInstanceIds = activityExecutionRecords.Select(x => x.WorkflowInstanceId).ToHashSet();
            
            if (workflowInstanceIds.Count == 0)
                workflowInstanceIds = matchingWorkflowInstanceIds;
            else
                workflowInstanceIds.IntersectWith(matchingWorkflowInstanceIds);
        }

        return workflowInstanceIds;
    }
}
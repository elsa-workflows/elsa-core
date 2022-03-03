using System.Linq;
using Elsa.Models;
using Elsa.Retention.Abstractions;
using Elsa.Retention.Models;

namespace Elsa.Retention.Filters;

/// <summary>
/// A filter that causes workflows that match any of the specified workflow statuses to be deleted.
/// If no statuses are specified, any workflows matching either the Finished or Cancelled status will be deleted. 
/// </summary>
public class CompletedWorkflowFilter : RetentionFilter
{
    private readonly WorkflowStatus[] _statuses;
    public CompletedWorkflowFilter(params WorkflowStatus[] statuses) => _statuses = statuses;

    public CompletedWorkflowFilter() : this(WorkflowStatus.Finished, WorkflowStatus.Cancelled)
    {
    }

    protected override bool GetShouldDelete(RetentionFilterContext context) => _statuses.Any(x => context.WorkflowInstance.WorkflowStatus == x);
}
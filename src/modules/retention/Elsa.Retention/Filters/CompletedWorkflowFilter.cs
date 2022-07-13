using Elsa.Models;

namespace Elsa.Retention.Filters;

/// <summary>
/// A filter that causes workflows that are either finished or cancelled to be deleted.
/// </summary>
public class CompletedWorkflowFilter : WorkflowStatusFilter
{
    public CompletedWorkflowFilter() : base(WorkflowStatus.Finished, WorkflowStatus.Cancelled)
    {
    }
}
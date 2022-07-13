using System.Linq;
using Elsa.Models;
using Elsa.Retention.Abstractions;
using Elsa.Retention.Models;

namespace Elsa.Retention.Filters;

/// <summary>
/// A filter that causes workflows that match any of the specified workflow statuses to be deleted.
/// </summary>
public class WorkflowStatusFilter : RetentionFilter
{
    private readonly WorkflowStatus[] _statuses;
    public WorkflowStatusFilter(params WorkflowStatus[] statuses) => _statuses = statuses;
    protected override bool GetShouldDelete(RetentionFilterContext context) => _statuses.Any(x => context.WorkflowInstance.WorkflowStatus == x);
}
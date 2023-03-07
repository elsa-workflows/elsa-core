using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// The scheduler contains work items to execute.
/// It continuously takes the next work item from the list until there are no more items left.
/// </summary>
public interface IActivityScheduler
{
    bool HasAny { get; }
    void Schedule(ActivityWorkItem workItem);
    ActivityWorkItem Take();
    IEnumerable<ActivityWorkItem> List();
    void Clear();
}
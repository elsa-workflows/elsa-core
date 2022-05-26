using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// The scheduler is a stack-based list containing work items to execute.
/// It continuously pops the next work item from the stack until there are no more items left.
/// </summary>
public interface IActivityScheduler
{
    bool HasAny { get; }
    void Push(ActivityWorkItem workItem);
    ActivityWorkItem Pop();
    IEnumerable<ActivityWorkItem> List();
    void Clear();
}
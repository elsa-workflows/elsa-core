using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// The scheduler contains work items to execute.
/// It continuously takes the next work item from the list until there are no more items left.
/// </summary>
public interface IActivityScheduler
{
    /// <summary>
    /// Returns true if there are any work items in the scheduler.
    /// </summary>
    bool HasAny { get; }
    
    /// <summary>
    /// Schedules a work item.
    /// </summary>
    /// <param name="workItem"></param>
    void Schedule(ActivityWorkItem workItem);
    
    /// <summary>
    /// Takes the next work item from the scheduler.
    /// </summary>
    /// <returns></returns>
    ActivityWorkItem Take();
    
    /// <summary>
    /// Returns a list of all work items in the scheduler.
    /// </summary>
    /// <returns></returns>
    IEnumerable<ActivityWorkItem> List();
    
    /// <summary>
    /// Clears all work items from the scheduler.
    /// </summary>
    void Clear();
}
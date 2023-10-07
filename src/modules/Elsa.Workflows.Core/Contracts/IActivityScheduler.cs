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
    /// <param name="workItem">The work item to schedule.</param>
    void Schedule(ActivityWorkItem workItem);
    
    /// <summary>
    /// Takes the next work item from the scheduler.
    /// </summary>
    ActivityWorkItem Take();
    
    /// <summary>
    /// Returns a list of all work items in the scheduler.
    /// </summary>
    IEnumerable<ActivityWorkItem> List();
    
    /// <summary>
    /// Returns true if there are any work items matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    bool Any(Func<ActivityWorkItem, bool> predicate);
    
    /// <summary>
    /// Returns the first work item matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    ActivityWorkItem? Find(Func<ActivityWorkItem, bool> predicate);
    
    /// <summary>
    /// Clears all work items from the scheduler.
    /// </summary>
    void Clear();
}
namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Implement this method if your activity needs to provide bookmark data that will be used when it is marked as a trigger. 
/// </summary>
public interface ITrigger : IActivity
{
    /// <summary>
    /// Implementors should return a list of objects where each object represents a bookmark datum. For each datum, a trigger is created.
    /// </summary>
    ValueTask<IEnumerable<object>> GetTriggerPayloadsAsync(TriggerIndexingContext context);
}
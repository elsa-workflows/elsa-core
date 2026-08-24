using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Implement this method if your activity needs to provide bookmark data that will be used when it is marked as a trigger. 
/// </summary>
public interface ITrigger : IActivity
{
    /// <summary>
    /// Implementors should return a list of objects where each object represents a bookmark datum. For each datum, a trigger is created.
    /// </summary>
    /// <remarks>
    /// Each returned object is stored under the stimulus name held by <see cref="TriggerIndexingContext.TriggerName"/>, which is shared by all payloads.
    /// To register payloads under more than one stimulus name, return <see cref="NamedTriggerPayload"/> instances: each carries the name for its own payload.
    /// </remarks>
    ValueTask<IEnumerable<object>> GetTriggerPayloadsAsync(TriggerIndexingContext context);
}
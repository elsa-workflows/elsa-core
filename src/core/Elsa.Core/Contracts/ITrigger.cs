using Elsa.Models;

namespace Elsa.Contracts;

/// <summary>
/// Activities that act as a trigger should implement this interface. 
/// </summary>
public interface ITrigger : INode
{
    /// <summary>
    /// A value indicating whether this trigger can create a new workflow instances when triggered, or execute the workflow instance this trigger belongs to.
    /// </summary>
    TriggerMode TriggerMode { get; set; }
    
    /// <summary>
    /// Implementing triggers should return zero or more objects that represent a lookup value.
    /// </summary>
    ///<example>
    /// For example, if your trigger can be configured with a signal name to listen for, you would return an object with said signal name as a field.
    /// Your auxiliary services would then use this signal name to create a hash and use it to lookup a trigger stored in the database by this hash.
    /// </example>
    ValueTask<IEnumerable<object>> GetTriggerPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken = default);
}
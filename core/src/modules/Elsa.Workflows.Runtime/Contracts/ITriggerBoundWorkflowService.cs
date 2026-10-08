namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a service that looks up trigger-bound workflows.
/// </summary>
public interface ITriggerBoundWorkflowService
{
    /// <summary>
    /// Finds trigger-bound workflows by activity type name and stimulus.
    /// </summary>
    Task<IEnumerable<TriggerBoundWorkflow>> FindManyAsync(string activityTypeName, object stimulus, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds trigger-bound workflows by stimulus hash.
    /// </summary>
    Task<IEnumerable<TriggerBoundWorkflow>> FindManyAsync(string stimulusHash, CancellationToken cancellationToken = default);
}
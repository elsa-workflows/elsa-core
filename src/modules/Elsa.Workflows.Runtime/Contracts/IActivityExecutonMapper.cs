using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Maps activity execution contexts to activity execution records.
/// </summary>
public interface IActivityExecutionMapper
{
    /// <summary>
    /// Maps an activity execution context to an activity execution record.
    /// </summary>
    Task<ActivityExecutionRecord> MapAsync(ActivityExecutionContext source);

    /// <summary>
    /// Retrieves a dictionary containing the persistable output of an activity execution context.
    /// </summary>
    /// <param name="context">The activity execution context to extract persistable output from.</param>
    /// <returns>A dictionary containing the persistable output.</returns>
    Task<Dictionary<string, object?>> GetPersistableOutputAsync(ActivityExecutionContext context);
}
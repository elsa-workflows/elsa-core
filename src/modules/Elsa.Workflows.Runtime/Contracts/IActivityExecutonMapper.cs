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
}
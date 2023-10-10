using Elsa.Workflows.Core;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Maps activity execution contexts to activity execution records.
/// </summary>
public interface IActivityExecutionMapper
{
    /// <summary>
    /// Maps an activity execution context to an activity execution record.
    /// </summary>
    ActivityExecutionRecord Map(ActivityExecutionContext source);
}
namespace Elsa.Api.Client.Resources.ActivityExecutions.Models;

/// <summary>
/// Represents a report of activity executions.
/// </summary>
/// <param name="Stats">The activity execution stats.</param>
public record ActivityExecutionReport(ICollection<ActivityExecutionStats> Stats);
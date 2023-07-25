using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Api.RealTime.Messages;

/// <summary>
/// Contains information about the activity execution logs updated event.
/// </summary>
/// <param name="Stats">Execution stats about a set of activities.</param>
public record ActivityExecutionLogUpdatedMessage(ICollection<ActivityExecutionStats> Stats);
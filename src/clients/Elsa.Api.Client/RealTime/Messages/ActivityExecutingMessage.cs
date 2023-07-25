using Elsa.Api.Client.Resources.ActivityExecutions.Models;

namespace Elsa.Api.Client.RealTime.Messages;

/// <summary>
/// Contains information about the activity executing event.
/// </summary>
/// <param name="ActivityId">The ID of the activity.</param>
/// <param name="Stats">Execution stats about the activity.</param>
public record ActivityExecutingMessage(string ActivityId, ActivityExecutionStats Stats);
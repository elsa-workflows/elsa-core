namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents a record of an activity's output.
/// </summary>
/// <param name="ContainerId">The ID of the container that contains the activity.</param>
/// <param name="ActivityId">The ID of the activity.</param>
/// <param name="ActivityInstanceId">The ID of the activity instance.</param>
/// <param name="OutputName">The name of the output.</param>
/// <param name="Value">The output value.</param>
public record ActivityOutputRecord(string ContainerId, string ActivityId, string ActivityInstanceId, string OutputName, object? Value);
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents an embedded activity.
/// </summary>
/// <param name="Activity">The embedded activity.</param>
/// <param name="PropertyName">The name of the property that contains the embedded activity.</param>
public record EmbeddedActivity(JsonObject Activity, string PropertyName)
{
    /// <summary>
    /// Stable logical owner identity for row-owned children, when the resolver has one.
    /// </summary>
    public string? StepId { get; init; }

    /// <summary>
    /// Stable role within a row-owned pair, when the resolver has one.
    /// </summary>
    public string? Role { get; init; }
}

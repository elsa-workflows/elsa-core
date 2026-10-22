using System.Text.Json.Serialization;

namespace Elsa.Studio.Bpmn.Designers;

/// <summary>
/// Instance state for one element, as a designer's <c>applyOverlays</c> takes it. Every field is optional: what a
/// projection cannot compute, it leaves null, and a null field is left out of what the designer receives.
/// </summary>
public sealed record BpmnDesignerElementStats
{
    /// <summary>How many tokens have entered the element.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Started { get; init; }

    /// <summary>How many tokens have left the element having completed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Completed { get; init; }

    /// <summary>How many tokens are on the element now.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Active { get; init; }

    /// <summary>Whether a token is waiting here for something external.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Blocked { get; init; }

    /// <summary>Whether execution faulted at the element.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Faulted { get; init; }

    /// <summary>Whether a token here was cancelled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Canceled { get; init; }
}

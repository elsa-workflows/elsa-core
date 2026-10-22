using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities.Compensation;

/// <summary>
/// An effect-free draft container for authored forward and recovery activity pairs.
/// </summary>
[Activity("Elsa", "Flow", "Authors paired forward and recovery activities. This draft-only scope cannot be published or executed.")]
[PublicAPI]
public sealed class CompensationScope : Activity
{
    public const string ActivityTypeName = "Elsa.CompensationScope";
    public const int CurrentDraftSchemaVersion = 1;
    public const string DraftOnlyRefusalMessage = "Native compensation scopes are draft-only and cannot be published or executed.";
    public const string DraftOnlyDiagnosticCode = "NATIVE_COMPENSATION_DRAFT_ONLY";

    /// <inheritdoc />
    public CompensationScope([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// Gets the version of the authored draft data shape.
    /// </summary>
    public int DraftSchemaVersion { get; set; } = CurrentDraftSchemaVersion;

    /// <summary>
    /// Gets or sets the ordered authored forward and recovery pairs.
    /// </summary>
    public ICollection<CompensationStep> Steps { get; set; } = new List<CompensationStep>();

    /// <summary>
    /// Gets or sets the O3 forward-fault handoff selection. It is authored only in this slice.
    /// </summary>
    public string OnForwardFault { get; set; } = CompensationFaultHandoff.Compensated;

    /// <summary>
    /// Preserves extension fields while a supported draft is materialized and saved.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }

    /// <summary>
    /// Exposes the owned activities to Core graph construction without duplicating them in JSON.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public IEnumerable<IActivity> Activities => Steps.SelectMany(step => new[] { step.Forward, step.Handler }).OfType<IActivity>();

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context) =>
        ValueTask.FromException(new InvalidOperationException(DraftOnlyRefusalMessage));
}

/// <summary>
/// One authored pair. Step identity is independent of each child's Elsa activity identity.
/// </summary>
[PublicAPI]
public sealed class CompensationStep
{
    /// <summary>
    /// Gets or sets the stable logical identity for this authored row.
    /// </summary>
    public string? StepId { get; set; }

    /// <summary>
    /// Gets or sets the editable row label.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the forward child. Null is retained for incomplete drafts.
    /// </summary>
    public IActivity? Forward { get; set; }

    /// <summary>
    /// Gets or sets the optional inline recovery child.
    /// </summary>
    public IActivity? Handler { get; set; }

    /// <summary>
    /// Retains existing capture mapping JSON without authoring or interpreting it in this slice.
    /// </summary>
    public JsonElement[] Mappings { get; set; } = [];

    /// <summary>
    /// Preserves extension fields while a supported draft is materialized and saved.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

/// <summary>
/// Authored O3 handoff values. Neither value enables execution in this draft-only implementation.
/// </summary>
public static class CompensationFaultHandoff
{
    public const string Compensated = "Compensated";
    public const string PropagateOriginalFault = "PropagateOriginalFault";
}

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Workflow properties the runtime writes when it starts a workflow from a stored trigger.
/// </summary>
/// <remarks>
/// <para>
/// One trigger activity can register several stored triggers, one per stimulus it answers, and the runtime starts the
/// workflow on that activity whichever of them matched. <see cref="StimulusHash"/> tells the started workflow which
/// one it was. Read it with <c>WorkflowExecutionContext.GetTriggerStimulusHash()</c>.
/// </para>
/// <para>
/// The property is written by every runtime path that starts a workflow from a stored trigger it has matched:
/// <see cref="IStimulusSender"/>, which also serves dispatched stimuli and published events; the trigger schedules of
/// <c>Elsa.Scheduling</c>; the HTTP endpoint middleware; and the MassTransit trigger dispatcher. It is a workflow
/// property, so it survives dispatch and is persisted with the instance.
/// </para>
/// </remarks>
public static class TriggerWorkflowProperties
{
    /// <summary>
    /// The workflow property holding the <see cref="Entities.StoredTrigger.Hash"/> of the stored trigger that started
    /// the workflow.
    /// </summary>
    public const string StimulusHash = "Elsa:TriggerStimulusHash";

    /// <summary>
    /// Returns a copy of <paramref name="properties"/> that also carries <paramref name="stimulusHash"/> under
    /// <see cref="StimulusHash"/>. The given dictionary is never modified.
    /// </summary>
    /// <param name="properties">The workflow properties supplied by the caller, if any.</param>
    /// <param name="stimulusHash">The hash of the stored trigger that matched, if any.</param>
    /// <returns>The copy, or <paramref name="properties"/> itself when there is no hash to add.</returns>
    public static IDictionary<string, object>? WithStimulusHash(IDictionary<string, object>? properties, string? stimulusHash)
    {
        if (string.IsNullOrWhiteSpace(stimulusHash))
            return properties;

        var copy = properties != null ? new Dictionary<string, object>(properties) : new Dictionary<string, object>();
        copy[StimulusHash] = stimulusHash;
        return copy;
    }
}

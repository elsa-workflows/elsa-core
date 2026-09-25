using System.Linq;
using System.Text.Json;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Folds the BPMN diagnostics projected onto a workflow instance's journal (see
/// <see cref="BpmnDiagnosticEventNames"/> and <see cref="BpmnDiagnosticLogPayload"/>) into a single element-keyed
/// <see cref="BpmnElementStats"/> map.
/// </summary>
/// <remarks>
/// The one place the mapping from a diagnostic kind to a stats change lives: nothing on the ClientLib side repeats
/// it, so a BPMN element's badge and a sequence flow's "taken" styling always agree with what this class decided.
/// Entries from a nested scope (each nested <c>BpmnProcess</c> writes diagnostics on its own activity) fold into
/// the very same map, because BPMN element and flow ids are unique across the whole document, not merely within
/// one scope.
/// </remarks>
public static class BpmnElementStatsProjector
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Projects the BPMN diagnostic entries in <paramref name="journalEntries"/> into an element-keyed stats map.
    /// Entries whose <c>Source</c> is not <see cref="BpmnDiagnosticEventNames.Source"/>, or whose payload does not
    /// deserialize as a <see cref="BpmnDiagnosticLogPayload"/>, are ignored, so callers may pass an unfiltered
    /// journal page.
    /// </summary>
    public static IReadOnlyDictionary<string, BpmnElementStats> Project(IEnumerable<WorkflowExecutionLogRecord> journalEntries)
    {
        var stats = new Dictionary<string, BpmnElementStats>();
        Fold(journalEntries, stats);
        return stats;
    }

    /// <summary>
    /// Folds <paramref name="journalEntries"/> into an already-populated element-keyed stats map, mutating it in
    /// place, so a caller that has already folded earlier journal pages need only fold the new ones rather than
    /// re-fold the whole history on every refresh.
    /// </summary>
    public static void Fold(IEnumerable<WorkflowExecutionLogRecord> journalEntries, Dictionary<string, BpmnElementStats> stats)
    {
        foreach (var entry in journalEntries.Where(e => e.Source == BpmnDiagnosticEventNames.Source))
        {
            if (!TryReadPayload(entry.Payload, out var payload))
                continue;

            if (!string.IsNullOrEmpty(payload.ElementId))
                ApplyToElement(GetOrAdd(stats, payload.ElementId), payload.Kind);

            // A diagnostic that names a flow id represents that flow having just been taken -- a sequence flow
            // has no state beyond that -- regardless of which kind carried it (normally TokenEmitted).
            if (!string.IsNullOrEmpty(payload.FlowId))
                ApplyToFlow(GetOrAdd(stats, payload.FlowId));
        }
    }

    private static BpmnElementStats GetOrAdd(Dictionary<string, BpmnElementStats> stats, string id)
    {
        if (stats.TryGetValue(id, out var entry))
            return entry;

        entry = new BpmnElementStats();
        stats[id] = entry;
        return entry;
    }

    /// <summary>
    /// Applies one diagnostic kind's effect on the element (or flow-target) it names. See the parenthetical list in
    /// the design: token emitted/consumed change how many tokens are present; a join that is <c>Waiting</c> is
    /// blocked, and stops being blocked once it is <c>Joined</c>; work being scheduled, completed or torn down
    /// moves the same active/completed counters bound work's own <c>ActivityStats</c> reports, so an element with
    /// no activity id still tells the same story.
    /// </summary>
    private static void ApplyToElement(BpmnElementStats stats, string kind)
    {
        switch (kind)
        {
            case BpmnDiagnosticEventNames.TokenEmitted:
            case BpmnDiagnosticEventNames.Scheduled:
            case BpmnDiagnosticEventNames.EventSubprocessActivated:
            case BpmnDiagnosticEventNames.CompensationTriggered:
            case BpmnDiagnosticEventNames.ScopeListenerArmed:
                stats.Started = (stats.Started ?? 0) + 1;
                stats.Active = (stats.Active ?? 0) + 1;
                break;

            case BpmnDiagnosticEventNames.Waiting:
                // A join arrived and is waiting on its siblings: a token is present (blocked), not yet consumed.
                stats.Blocked = true;
                break;

            case BpmnDiagnosticEventNames.Joined:
                stats.Blocked = false;
                stats.Completed = (stats.Completed ?? 0) + 1;
                stats.Active = Decrement(stats.Active);
                break;

            case BpmnDiagnosticEventNames.Consumed:
            case BpmnDiagnosticEventNames.Terminated:
            case BpmnDiagnosticEventNames.EventSubprocessCompleted:
            case BpmnDiagnosticEventNames.Compensated:
            case BpmnDiagnosticEventNames.EscalationCaught:
            case BpmnDiagnosticEventNames.ScopeListenerFired:
                stats.Completed = (stats.Completed ?? 0) + 1;
                stats.Active = Decrement(stats.Active);
                break;

            case BpmnDiagnosticEventNames.ScopeListenerRetired:
                stats.Active = Decrement(stats.Active);
                break;

            case BpmnDiagnosticEventNames.Canceled:
            case BpmnDiagnosticEventNames.TransactionCancelled:
                stats.Canceled = true;
                stats.Active = Decrement(stats.Active);
                break;

            case BpmnDiagnosticEventNames.Faulted:
            case BpmnDiagnosticEventNames.BehaviorFailure:
            case BpmnDiagnosticEventNames.CallActivityFailureRouted:
                stats.Faulted = true;
                break;

            // CompensationRegistered, EscalationRaised, EscalationUnhandled and EscalationLate are bookkeeping
            // that leaves no per-element state worth showing; EscalationUnhandled and EscalationLate are
            // documented as never a fault, so they must not be folded into Faulted.
            default:
                break;
        }
    }

    private static void ApplyToFlow(BpmnElementStats stats)
    {
        stats.Started = (stats.Started ?? 0) + 1;
        stats.Completed = (stats.Completed ?? 0) + 1;
    }

    private static int Decrement(int? value) => Math.Max(0, (value ?? 0) - 1);

    private static bool TryReadPayload(object? payload, out BpmnDiagnosticLogPayload result)
    {
        switch (payload)
        {
            case BpmnDiagnosticLogPayload direct:
                result = direct;
                return true;

            case JsonElement { ValueKind: JsonValueKind.Object } element:
                var deserialized = element.Deserialize<BpmnDiagnosticLogPayload>(PayloadSerializerOptions);

                if (deserialized != null)
                {
                    result = deserialized;
                    return true;
                }

                break;
        }

        result = null!;
        return false;
    }
}

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Instance state for one BPMN element or sequence flow, keyed by its BPMN document id -- an element id, or, for a
/// sequence flow, a flow id, since element and flow ids are both document-unique.
/// </summary>
/// <remarks>
/// This is a write-only projection derived from diagnostics: <see cref="BpmnElementStatsProjector"/> is the only
/// thing that produces it, and nothing reads it back into the engine or edits it. It exists so a gateway, an
/// intermediate event or a sequence flow -- none of which has an Elsa activity id under Option A -- has something
/// for the instance viewer's overlay to show, alongside the existing activity-keyed <see cref="ActivityStats"/> for
/// bound work. Mirrors the ClientLib's canvas-neutral <c>BpmnElementStats</c> interface (<c>src/bpmn/model.ts</c>),
/// which owns the shape from the rendering side; every field here is nullable for the same reason that one is
/// all-optional: a projection that cannot compute a field says nothing about it rather than reporting a zero.
/// </remarks>
public sealed class BpmnElementStats
{
    /// <summary>How many tokens have entered this element or been carried by this flow.</summary>
    public int? Started { get; set; }

    /// <summary>How many tokens have left this element having completed, or how many times this flow was taken.</summary>
    public int? Completed { get; set; }

    /// <summary>How many tokens are sitting on this element right now (a waiting catch event, an armed listener).</summary>
    public int? Active { get; set; }

    /// <summary>Whether a token is parked here waiting for something external (e.g. a join awaiting its siblings).</summary>
    public bool? Blocked { get; set; }

    /// <summary>Whether execution faulted at this element.</summary>
    public bool? Faulted { get; set; }

    /// <summary>Whether a token here was cancelled (an interrupted activity, a lost event race, a torn-down scope).</summary>
    public bool? Canceled { get; set; }
}

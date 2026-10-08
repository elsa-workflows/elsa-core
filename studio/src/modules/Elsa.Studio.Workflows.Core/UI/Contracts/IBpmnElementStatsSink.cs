using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// An optional capability an <see cref="IDiagramDesigner"/> may implement to accept the element-keyed instance
/// overlay: a gateway, an intermediate event or a sequence flow has no Elsa activity id under BPMN's Option A, so
/// <see cref="IDiagramDesigner.UpdateActivityStatsAsync"/> alone never lights one up.
/// </summary>
/// <remarks>
/// Kept as a separate interface, rather than a member on <see cref="IDiagramDesigner"/> itself, so that every other
/// diagram designer -- which has no BPMN element ids to key anything on -- is untouched by this projection.
/// </remarks>
public interface IBpmnElementStatsSink
{
    /// <summary>
    /// Replaces the whole element-keyed instance overlay. The map is authoritative: an element or flow it no
    /// longer mentions loses its badge or "taken" styling.
    /// </summary>
    /// <param name="elementStats">The stats, keyed by BPMN element id or, for a sequence flow, by flow id.</param>
    Task UpdateElementStatsAsync(IReadOnlyDictionary<string, BpmnElementStats> elementStats);
}

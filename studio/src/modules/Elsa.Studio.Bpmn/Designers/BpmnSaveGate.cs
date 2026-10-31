namespace Elsa.Studio.Bpmn.Designers;

/// <summary>
/// Decides whether the host may send a BPMN designer's saved document: before the first save after a load that
/// reported dropped content, the user has to acknowledge what will be lost.
/// </summary>
/// <remarks>
/// The designer takes no part in this. Its <c>save</c> always returns the XML; the host keeps one gate per open
/// document, shows <see cref="DroppedWarnings"/> while <see cref="RequiresAcknowledgement"/> is set, and calls
/// <see cref="Acknowledge"/> when the user accepts. Rewritten content needs no acknowledgement, because a save keeps it.
/// </remarks>
public sealed class BpmnSaveGate
{
    private IReadOnlyList<BpmnDesignerLoadWarning> _droppedWarnings = [];
    private bool _acknowledged;

    /// <summary>The warnings of the last load that report content a save would lose, in the order the designer reported them.</summary>
    public IReadOnlyList<BpmnDesignerLoadWarning> DroppedWarnings => _droppedWarnings;

    /// <summary>Whether the user still has to acknowledge <see cref="DroppedWarnings"/> before the document may be saved.</summary>
    public bool RequiresAcknowledgement => _droppedWarnings.Count > 0 && !_acknowledged;

    /// <summary>Starts over for a newly loaded document, with the warnings its load returned.</summary>
    public void Loaded(IEnumerable<BpmnDesignerLoadWarning> warnings)
    {
        _droppedWarnings = warnings.Where(warning => warning.IsDropped).ToList();
        _acknowledged = false;
    }

    /// <summary>Records that the user accepted losing <see cref="DroppedWarnings"/>. Holds until the next load.</summary>
    public void Acknowledge() => _acknowledged = true;
}

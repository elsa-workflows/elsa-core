namespace Elsa.Studio.Bpmn.Designers;

/// <summary>
/// The BPMN designer a Studio feature falls back to when the host enables none, such as bpmn-js for the workflows
/// module. It is not an enabled designer: a host that enables any designer uses those, and this one is ignored.
/// </summary>
/// <param name="Designer">The designer to fall back to.</param>
public sealed record DefaultBpmnDesigner(BpmnDesignerDescriptor Designer);

namespace Elsa.Studio.Bpmn.Designers;

/// <summary>Chooses the BPMN designer when a host enables more than one.</summary>
public class BpmnDesignerOptions
{
    /// <summary>
    /// The <see cref="BpmnDesignerDescriptor.DesignerId"/> of the designer to use. Required when a host enables more than
    /// one designer; when it enables one, it must be null or name that one.
    /// </summary>
    public string? DesignerId { get; set; }
}

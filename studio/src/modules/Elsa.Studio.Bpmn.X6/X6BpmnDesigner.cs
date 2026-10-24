using Elsa.Studio.Bpmn.Designers;

namespace Elsa.Studio.Bpmn.X6;

/// <summary>The X6 BPMN designer.</summary>
public static class X6BpmnDesigner
{
    /// <summary>The id the designer's module declares, which <see cref="BpmnDesignerOptions.DesignerId"/> chooses it by.</summary>
    public const string DesignerId = "x6";

    /// <summary>The designer's module and stylesheet, as ClientLib/build.mjs writes them to this project's wwwroot.</summary>
    public static BpmnDesignerDescriptor Descriptor { get; } = new(
        DesignerId,
        "_content/Elsa.Studio.Bpmn.X6/x6-designer.js",
        "_content/Elsa.Studio.Bpmn.X6/x6-designer.css");
}

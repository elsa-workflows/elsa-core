using Elsa.Studio.Bpmn.Designers;

namespace Elsa.Studio.Bpmn.BpmnJs;

/// <summary>The bpmn-js BPMN designer.</summary>
public static class BpmnJsDesigner
{
    /// <summary>The id the designer's module declares, which <see cref="BpmnDesignerOptions.DesignerId"/> chooses it by.</summary>
    public const string DesignerId = "bpmn-js";

    /// <summary>The designer's module and stylesheet, as ClientLib/build.mjs writes them to this project's wwwroot.</summary>
    public static BpmnDesignerDescriptor Descriptor { get; } = new(
        DesignerId,
        "_content/Elsa.Studio.Bpmn.BpmnJs/bpmn-js-designer.js",
        "_content/Elsa.Studio.Bpmn.BpmnJs/bpmn-js-designer.css");
}

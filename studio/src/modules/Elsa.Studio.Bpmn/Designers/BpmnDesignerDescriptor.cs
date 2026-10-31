namespace Elsa.Studio.Bpmn.Designers;

/// <summary>
/// A BPMN designer a host has enabled: the JavaScript module that implements the BPMN designer contract, and the
/// stylesheet it draws with.
/// </summary>
/// <param name="DesignerId">The id the module declares as its <c>designerId</c>, such as <c>bpmn-js</c>. Hosts choose a designer by this id.</param>
/// <param name="ModuleUrl">The module's URL, relative to the page's base URL, such as <c>_content/Elsa.Studio.Bpmn.BpmnJs/bpmn-js-designer.js</c>.</param>
/// <param name="StylesheetUrl">The stylesheet's URL, relative to the page's base URL, or null when the module brings its own styles.</param>
public sealed record BpmnDesignerDescriptor(string DesignerId, string ModuleUrl, string? StylesheetUrl = null);

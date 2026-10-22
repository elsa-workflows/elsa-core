namespace Elsa.Studio.Bpmn.Designers;

/// <summary>An element's <c>elsa:activityBinding</c>, as a designer's <c>getBinding</c> and <c>setBinding</c> exchange it.</summary>
/// <param name="ActivityType">The activity registry's type name the binding names.</param>
/// <param name="Inputs">Each configured input, in document order.</param>
public sealed record BpmnDesignerBinding(string ActivityType, IReadOnlyList<BpmnDesignerBindingInput> Inputs);

/// <summary>One configured input of a <see cref="BpmnDesignerBinding"/>.</summary>
/// <param name="Name">The input's camelCase property name.</param>
/// <param name="Value">The input's JSON text, as the document stores it.</param>
public sealed record BpmnDesignerBindingInput(string Name, string Value);

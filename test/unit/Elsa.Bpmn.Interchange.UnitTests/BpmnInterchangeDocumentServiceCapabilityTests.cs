using Bpmn.Interchange;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Interchange.Services;

namespace Elsa.Bpmn.Interchange.UnitTests;

/// <summary>
/// Capability refusal at import: <see cref="BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied"/> is the
/// internal seam the Import endpoint calls into, exercised here directly because the current library version defines
/// no capability <see cref="BpmnInterchangeDocumentService.DeclaredHostCapabilities"/> does not already cover, so a
/// document genuinely refused by import cannot be produced through the public API today.
/// </summary>
public class BpmnInterchangeDocumentServiceCapabilityTests
{
    [Fact(DisplayName = "A definition needing a capability the host does not declare is refused, naming the capability and the driving element")]
    public void EnsureCapabilitiesSatisfied_RefusesADefinitionNeedingAnUndeclaredCapability()
    {
        var definition = MultiInstanceDefinition("main", "each");

        var exception = Assert.Throws<BpmnCapabilityException>(() =>
            BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied(definition, [], BpmnHostCapabilities.None));

        Assert.Equal(BpmnHostCapabilities.IterationScopes, exception.Missing);
        Assert.Contains("each", exception.DrivingElementIds);
    }

    [Fact(DisplayName = "The same definition is accepted once the host declares the capability it needs")]
    public void EnsureCapabilitiesSatisfied_AcceptsADefinitionOnceTheCapabilityIsDeclared()
    {
        var definition = MultiInstanceDefinition("main", "each");

        // No exception is the assertion: the capability the definition needs is now declared.
        BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied(definition, [], BpmnHostCapabilities.IterationScopes);
    }

    [Fact(DisplayName = "A nested process needing an undeclared capability is refused even though the root process needs nothing")]
    public void EnsureCapabilitiesSatisfied_WalksIntoNestedProcesses()
    {
        var nestedDefinition = MultiInstanceDefinition("sub", "each");
        var rootDefinition = new BpmnProcessDefinition("main");

        BpmnWorkBinding[] bindings = [new BpmnWorkBinding.NestedProcess("main", "sub", "node-sub", BpmnBindingSlot.Primary, nestedDefinition)];

        var exception = Assert.Throws<BpmnCapabilityException>(() =>
            BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied(rootDefinition, bindings, BpmnHostCapabilities.None));

        Assert.Equal(BpmnHostCapabilities.IterationScopes, exception.Missing);
        Assert.Contains("each", exception.DrivingElementIds);
    }

    private static BpmnProcessDefinition MultiInstanceDefinition(string processId, string elementId)
    {
        var element = new BpmnElement(
            elementId,
            BpmnElementTypes.ServiceTask,
            bindingRef: $"node-{elementId}",
            loopCharacteristics: new BpmnLoopCharacteristics(isSequential: false, cardinality: 3));

        return new BpmnProcessDefinition(processId, Elements: [element]);
    }
}

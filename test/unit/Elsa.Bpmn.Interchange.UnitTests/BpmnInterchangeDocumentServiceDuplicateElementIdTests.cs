using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using System.Threading.Tasks;

namespace Elsa.Bpmn.Interchange.UnitTests;

/// <summary>
/// Duplicate element id refusal at import: <see cref="BpmnInterchangeDocumentService.EnsureElementIdsUnique"/> is the
/// internal seam the Import endpoint and the document <c>PUT</c> both call into, exercised here directly with the
/// exact shape a subprocess nested inside another subprocess that reuses its parent's id produces — the shape that
/// otherwise makes <see cref="BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied"/>, <c>BpmnWorkBinder.BindScope</c>
/// and <c>BpmnXmlWriter</c> recurse without terminating (elsa-core#8074).
/// </summary>
public class BpmnInterchangeDocumentServiceDuplicateElementIdTests
{
    [Test]
    [DisplayName("A document whose element ids are all unique is accepted")]
    public void EnsureElementIdsUnique_AcceptsADocumentWithNoRepeatedIds()
    {
        var task = new BpmnElement("task-1", BpmnElementTypes.ServiceTask, bindingRef: "node-task-1");
        var root = new BpmnProcessDefinition("main", Elements: [task]);

        // No exception is the assertion: every element id in the document is unique.
        BpmnInterchangeDocumentService.EnsureElementIdsUnique([root], []);
    }

    [Test]
    [DisplayName("A document declaring the same element id twice at the top level is refused, naming the id")]
    public async Task EnsureElementIdsUnique_RefusesARepeatedTopLevelElementId()
    {
        var first = new BpmnElement("dup", BpmnElementTypes.ServiceTask, bindingRef: "node-dup-1");
        var second = new BpmnElement("dup", BpmnElementTypes.ServiceTask, bindingRef: "node-dup-2");
        var root = new BpmnProcessDefinition("main", Elements: [first, second]);

        var exception = Assert.ThrowsExactly<BpmnDuplicateElementIdException>(() =>
            BpmnInterchangeDocumentService.EnsureElementIdsUnique([root], []));

        await Assert.That(exception.DuplicateElementIds).IsEquivalentTo(["dup"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(exception.Message).Contains("dup");
    }

    [Test]
    [DisplayName("A subprocess nested inside another subprocess that reuses its parent's id is refused, naming that id")]
    public async Task EnsureElementIdsUnique_RefusesASubprocessNestedInsideASubprocessThatReusesItsParentsId()
    {
        var (root, bindings) = NestedSubprocessReusingItsOwnId();

        var exception = Assert.ThrowsExactly<BpmnDuplicateElementIdException>(() =>
            BpmnInterchangeDocumentService.EnsureElementIdsUnique([root], bindings));

        await Assert.That(exception.DuplicateElementIds).IsEquivalentTo(["Outer"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("A subprocess reusing its parent top-level process's own id is refused, naming that id")]
    public async Task EnsureElementIdsUnique_RefusesASubprocessReusingItsParentTopLevelProcessesOwnId()
    {
        // <process id="P"><subProcess id="P">...</subProcess></process>: the subprocess element id equals the
        // top-level process's own ProcessId. Before the fix, a top-level process's own id was never added to the
        // pool checked for uniqueness (only its Elements were), so "P" appeared only once — as the subprocess
        // element inside root.Elements — and this collision went undetected.
        var subProcessElement = new BpmnElement("P", BpmnElementTypes.SubProcess, bindingRef: "node-p");
        var root = new BpmnProcessDefinition("P", Elements: [subProcessElement]);
        var subProcessBody = new BpmnProcessDefinition("P");

        BpmnWorkBinding[] bindings = [new BpmnWorkBinding.NestedProcess("P", "P", "node-p", BpmnBindingSlot.Primary, subProcessBody)];

        var exception = Assert.ThrowsExactly<BpmnDuplicateElementIdException>(() =>
            BpmnInterchangeDocumentService.EnsureElementIdsUnique([root], bindings));

        await Assert.That(exception.DuplicateElementIds).IsEquivalentTo(["P"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Two top-level processes sharing the same id are refused, naming that id")]
    public async Task EnsureElementIdsUnique_RefusesTwoTopLevelProcessesSharingAnId()
    {
        var first = new BpmnProcessDefinition("shared", Elements: [new BpmnElement("task-1", BpmnElementTypes.ServiceTask, bindingRef: "node-task-1")]);
        var second = new BpmnProcessDefinition("shared", Elements: [new BpmnElement("task-2", BpmnElementTypes.ServiceTask, bindingRef: "node-task-2")]);

        var exception = Assert.ThrowsExactly<BpmnDuplicateElementIdException>(() =>
            BpmnInterchangeDocumentService.EnsureElementIdsUnique([first, second], []));

        await Assert.That(exception.DuplicateElementIds).IsEquivalentTo(["shared"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    /// <summary>
    /// The exact document shape elsa-core#8074 reports: process <c>main</c> declares a subprocess <c>Outer</c>,
    /// whose body declares another subprocess that reuses the id <c>Outer</c> rather than declaring one of its own.
    /// </summary>
    private static (BpmnProcessDefinition Root, IReadOnlyList<BpmnWorkBinding> Bindings) NestedSubprocessReusingItsOwnId()
    {
        var outerElement = new BpmnElement("Outer", BpmnElementTypes.SubProcess, bindingRef: "node-outer");
        var root = new BpmnProcessDefinition("main", Elements: [outerElement]);

        // The inner subprocess element, declared inside Outer's own body, reuses "Outer" as its id instead of
        // declaring its own — the innermost NestedProcess.Definition's own ProcessId is "Outer" too, for the same
        // reason (Bpmn.Interchange's reader gives a subprocess body the element id that opens it as its ProcessId).
        var innerElement = new BpmnElement("Outer", BpmnElementTypes.SubProcess, bindingRef: "node-outer-inner");
        var outerBody = new BpmnProcessDefinition("Outer", Elements: [innerElement]);
        var innerBody = new BpmnProcessDefinition("Outer");

        BpmnWorkBinding[] bindings =
        [
            new BpmnWorkBinding.NestedProcess("main", "Outer", "node-outer", BpmnBindingSlot.Primary, outerBody),
            new BpmnWorkBinding.NestedProcess("Outer", "Outer", "node-outer-inner", BpmnBindingSlot.Primary, innerBody)
        ];

        return (root, bindings);
    }
}

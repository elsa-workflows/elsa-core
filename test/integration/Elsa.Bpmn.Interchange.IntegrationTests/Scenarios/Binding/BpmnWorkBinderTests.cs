using System.Text.Json;
using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Scheduling.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions.Enums;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;

/// <summary>
/// Turning <c>BpmnWorkBinding</c> declarations into the activities a <c>BpmnProcess</c> scope runs.
/// </summary>
public class BpmnWorkBinderTests : BpmnBindingTestBase
{
    private const string ProcessId = "main";

    [Test]
    [DisplayName("A timer wait binds to a Delay of the declared ISO-8601 duration")]
    public async Task TimerWait_BindsToADelay()
    {
        var scope = Bind(Definition(Element("wait", BpmnElementTypes.IntermediateCatchEvent)), Timer("wait", "PT5M"));

        var work = WorkOf(scope, "wait");
        await Assert.That(work).IsOfType(typeof(Delay));
        var delay = (Delay)work;

        await Assert.That(ValueOf<TimeSpan>(delay.TimeSpan)).IsEqualTo(TimeSpan.FromMinutes(5));
    }

    [Test]
    [DisplayName("A timer wait declaring something that is not an ISO-8601 duration is refused")]
    public async Task TimerWait_RefusesADurationElsaCannotWaitFor()
    {
        // A duration quietly defaulted to zero is a timer boundary event that fires the moment it is armed, which
        // cancels the task it guards before that task has done anything.
        var exception = Assert.ThrowsExactly<BpmnBindingException>(() => Bind(Definition(Element("wait", BpmnElementTypes.IntermediateCatchEvent)), Timer("wait", "5 minutes")));

        await Assert.That(exception.Message).Contains("5 minutes", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("A message wait binds to an Event on the message name")]
    public async Task MessageWait_BindsToAnEvent()
    {
        var scope = Bind(
            Definition(Element("receive", BpmnElementTypes.ReceiveTask)),
            new BpmnWorkBinding.MessageWait(ProcessId, "receive", Ref("receive"), BpmnBindingSlot.Primary, "OrderPlaced"));

        var work = WorkOf(scope, "receive");
        await Assert.That(work).IsOfType(typeof(Event));
        var @event = (Event)work;

        await Assert.That(ValueOf<string>(@event.EventName)).IsEqualTo("OrderPlaced");

        // A catch inside a scope is not a way into the workflow; only the process's own start events are, and saying
        // so is the enclosing BpmnProcess's decision, not this activity's.
        await Assert.That(@event.CanStartWorkflow).IsFalse();
    }

    [Test]
    [DisplayName("A signal wait binds to an Event on the signal name")]
    public async Task SignalWait_BindsToAnEvent()
    {
        var scope = Bind(
            Definition(Element("await", BpmnElementTypes.IntermediateCatchEvent)),
            new BpmnWorkBinding.SignalWait(ProcessId, "await", Ref("await"), BpmnBindingSlot.Primary, "Cancelled"));

        var work = WorkOf(scope, "await");
        await Assert.That(work).IsOfType(typeof(Event));
        var eventActivity = (Event)work;
        await Assert.That(ValueOf<string>(eventActivity.EventName)).IsEqualTo("Cancelled");
    }

    [Test]
    [DisplayName("A message publish binds to a PublishEvent on the message name")]
    public async Task MessagePublish_BindsToAPublishEvent()
    {
        var scope = Bind(
            Definition(Element("send", BpmnElementTypes.SendTask)),
            new BpmnWorkBinding.MessagePublish(ProcessId, "send", Ref("send"), BpmnBindingSlot.Primary, "OrderShipped"));

        var work = WorkOf(scope, "send");
        await Assert.That(work).IsOfType(typeof(PublishEvent));
        var publishEvent = (PublishEvent)work;
        await Assert.That(ValueOf<string>(publishEvent.EventName)).IsEqualTo("OrderShipped");
    }

    [Test]
    [DisplayName("A call activity binds to a DispatchWorkflow on the called element, keeping fire-and-forget")]
    public async Task CallProcess_BindsToADispatchWorkflow()
    {
        var scope = Bind(
            Definition(Element("call", BpmnElementTypes.CallActivity)),
            new BpmnWorkBinding.CallProcess(ProcessId, "call", Ref("call"), BpmnBindingSlot.Primary, "shipping-process", false));

        var work = WorkOf(scope, "call");
        await Assert.That(work).IsOfType(typeof(DispatchWorkflow));
        var dispatch = (DispatchWorkflow)work;

        await Assert.That(ValueOf<string>(dispatch.WorkflowDefinitionId)).IsEqualTo("shipping-process");

        // BPMN has no standard way to say "fire and forget", so the library carries it on the binding. Dropping it
        // here turns an asynchronous call into one the enclosing scope waits on, and the process stops where it used
        // to carry on.
        await Assert.That(ValueOf<bool>(dispatch.WaitForCompletion)).IsFalse();
    }

    [Test]
    [DisplayName("A call activity naming no called element is refused")]
    public void CallProcess_RefusesACallWithNothingToCall()
    {
        Assert.ThrowsExactly<BpmnBindingException>(() => Bind(
            Definition(Element("call", BpmnElementTypes.CallActivity)),
            new BpmnWorkBinding.CallProcess(ProcessId, "call", Ref("call"), BpmnBindingSlot.Primary, null, true)));
    }

    [Test]
    [DisplayName("A nested process binds to a BpmnProcess scope that binds its own work")]
    public async Task NestedProcess_BindsToANestedScope()
    {
        var body = new BpmnProcessDefinition("sub", Elements: [BoundElement("subWork", BpmnElementTypes.ServiceTask, new WriteLine("nested"))]);

        var scope = Bind(
            Definition(Element("sub", BpmnElementTypes.SubProcess)),
            new BpmnWorkBinding.NestedProcess(ProcessId, "sub", Ref("sub"), BpmnBindingSlot.Primary, body),
            Unbound("subWork", processId: "sub"));

        var nestedWork = WorkOf(scope, "sub");
        await Assert.That(nestedWork).IsOfType(typeof(BpmnProcess));
        var nested = (BpmnProcess)nestedWork;

        await Assert.That(nested.Process).IsSameReferenceAs(body);
        var nestedActivity = WorkOf(nested, "subWork");
        await Assert.That(nestedActivity).IsOfType(typeof(WriteLine));
        var writeLine = (WriteLine)nestedActivity;
        await Assert.That(ValueOf<string>(writeLine.Text)).IsEqualTo("nested");

        // A nested scope's start events are internal to the process around it. The command applier refuses a nested
        // scope that says otherwise, so a binder that marked one would only fail once the process ran.
        await Assert.That(nested.IsRootScope).IsFalse();
    }

    [Test]
    [DisplayName("Bind marks the one scope it returns directly as the workflow's root scope")]
    public async Task Bind_MarksTheReturnedScopeAsRootScope()
    {
        // Binding one process definition on its own is what turns an imported .bpmn document into a workflow's own
        // entry point -- Bind is the only call that gets to decide this, and it decides it every time.
        var scope = Bind(Definition(BoundElement("only", BpmnElementTypes.ServiceTask, new WriteLine("only"))), Unbound("only"));

        await Assert.That(scope.IsRootScope).IsTrue();
    }

    [Test]
    [DisplayName("A document-declared variable is copied onto the bound scope, and drives a collection-mode multi-instance")]
    public async Task DocumentDeclaredVariable_DrivesACollectionModeMultiInstance()
    {
        // BpmnScopeVariables.TryRead resolves purely through Elsa's own ExpressionExecutionContext.GetVariable, which
        // walks Container.Variables — never BpmnProcessDefinition.Variables. A collection the document declares but
        // the binder never copies onto the scope is Absent to the interpreter, and a collection-mode multi-instance
        // over it faults the element instead of running once per item. Running the bound scope for real, rather than
        // only inspecting its shape, is what catches that: a structural assertion on scope.Variables would still pass
        // if the interpreter could not actually see the value.
        const string collectionVariableName = "items";

        var each = new BpmnElement(
            "each",
            BpmnElementTypes.ServiceTask,
            bindingRef: Ref("each"),
            loopCharacteristics: new BpmnLoopCharacteristics(isSequential: false, collectionVariable: collectionVariableName),
            extensions: BpmnActivityBindingFormat.Attach(null, Format.Write(new WriteLine("iterated"))));
        var after = BoundElement("after", BpmnElementTypes.ServiceTask, new WriteLine("after"));

        var definition = new BpmnProcessBuilder(ProcessId)
            .Variable(collectionVariableName)
            .StartEvent("start")
            .Element(each)
            .Element(after)
            .EndEvent("end")
            .ConnectSequence("start", "each", "after", "end")
            .Build();

        // The default travels as the declaration's own JsonElement, seeded here the way an imported .bpmn would carry
        // it: the document names the variable and gives it a value, and the binder is what makes both visible.
        definition = definition with
        {
            Variables = [new BpmnVariableDeclaration(collectionVariableName, null, JsonSerializer.SerializeToElement(new[] { "alpha", "beta", "gamma" }))]
        };

        var scope = Bind(definition, Unbound("each"), Unbound("after"));

        await Assert.That(scope.Variables).Contains(variable => variable.Name == collectionVariableName);

        var eachActivityId = scope.WorkBindings[Ref("each")];
        var afterActivityId = scope.WorkBindings[Ref("after")];

        var result = await Services.GetRequiredService<IWorkflowRunner>().RunAsync(scope);

        await Assert.That(result.Journal.ActivityExecutionContexts.Count(context => context.Activity.Id == eachActivityId)).IsEqualTo(3);
        await Assert.That(result.Journal.ActivityExecutionContexts.Count(context => context.Activity.Id == afterActivityId)).IsEqualTo(1);
        await Assert.That(result.WorkflowState.Incidents).IsEmpty();
    }

    [Test]
    [DisplayName("A ScopeListener binding is bound like any other, under its own binding ref")]
    public async Task ScopeListenerSlot_BindsWithNoSpecialCase()
    {
        // An event subprocess element carries two bindings: its body, and the listener its enclosing scope arms while
        // it runs. Both are entries in the same map; the interpreter arms the listener at scope start on its own.
        var element = new BpmnElement(
            "escalationHandler",
            BpmnElementTypes.SubProcess,
            bindingRef: Ref("escalationHandler"),
            triggeredByEvent: true,
            listenerBindingRef: ListenerRef("escalationHandler"));

        var scope = Bind(
            Definition(element),
            new BpmnWorkBinding.NestedProcess(ProcessId, "escalationHandler", Ref("escalationHandler"), BpmnBindingSlot.Primary, new BpmnProcessDefinition("escalationHandler")),
            new BpmnWorkBinding.MessageWait(ProcessId, "escalationHandler", ListenerRef("escalationHandler"), BpmnBindingSlot.ScopeListener, "Escalated"));

        await Assert.That(WorkForRef(scope, Ref("escalationHandler"))).IsOfType(typeof(BpmnProcess));
        var listenerWork = WorkForRef(scope, ListenerRef("escalationHandler"));
        await Assert.That(listenerWork).IsOfType(typeof(Event));
        var escalationEvent = (Event)listenerWork;
        await Assert.That(ValueOf<string>(escalationEvent.EventName)).IsEqualTo("Escalated");
    }

    [Test]
    [DisplayName("A compensation handler is bound like any other work, and only compensation replay runs it")]
    public async Task CompensationHandler_IsBoundAndOnlyReachedByReplay()
    {
        // An isForCompensation element binds work and takes no sequence flows, and the reader emits an ordinary
        // Primary binding for it — so the binder needs no case for it, which is exactly why this is worth pinning.
        // A binder that skipped such a binding would still produce a scope that builds and publishes; the failure
        // would surface only once a replay asked for work the scope maps to nothing. Running the bound scope is what
        // turns that into an assertion.
        //
        // The throw names one of the two hosts, so the other handler is bound and reachable and still must not run:
        // nothing in the graph flows into a compensation handler, and only the replay's own selection reaches it.
        var definition = new BpmnProcessBuilder(ProcessId)
            .StartEvent("start")
            .Element(BoundElement("book", BpmnElementTypes.ServiceTask, new WriteLine("booked")))
            .Element(BoundElement("pay", BpmnElementTypes.ServiceTask, new WriteLine("paid")))
            .IntermediateThrowEvent("undoBookOnly", Compensation(activityRef: "book"))
            .EndEvent("end")
            .Element(CompensationBoundary("bookCompensated", attachedTo: "book", handler: "undoBook"))
            .Element(CompensationBoundary("payCompensated", attachedTo: "pay", handler: "undoPay"))
            .Element(CompensationHandler("undoBook", new WriteLine("unbooked")))
            .Element(CompensationHandler("undoPay", new WriteLine("unpaid")))
            .ConnectSequence("start", "book", "pay", "undoBookOnly", "end")
            .Build();

        var scope = Bind(definition, Unbound("book"), Unbound("pay"), Unbound("undoBook"), Unbound("undoPay"));

        // Bound: a handler is an entry in the same map as any other binding ref, under no special slot.
        var undoBookActivityId = scope.WorkBindings[Ref("undoBook")];
        var undoPayActivityId = scope.WorkBindings[Ref("undoPay")];

        var result = await Services.GetRequiredService<IWorkflowRunner>().RunAsync(scope);

        await Assert.That(result.Journal.ActivityExecutionContexts.Count(context => context.Activity.Id == undoBookActivityId)).IsEqualTo(1);
        await Assert.That(result.Journal.ActivityExecutionContexts.Count(context => context.Activity.Id == undoPayActivityId)).IsEqualTo(0);
        await Assert.That(result.WorkflowState.Incidents).IsEmpty();
    }

    [Test]
    [DisplayName("An unbound task the document does not bind is refused, naming the element")]
    public async Task UnboundTask_WithNoDeclarationIsRefused()
    {
        // The failure that has to be loud. Skipping the binding instead would produce a scope whose interpreter
        // schedules work for a binding ref nothing maps, which surfaces much later and much further away.
        var exception = Assert.ThrowsExactly<BpmnBindingException>(() => Bind(Definition(Element("approve", BpmnElementTypes.UserTask)), Unbound("approve")));

        await Assert.That(exception.Message).Contains("approve", StringComparison.CurrentCulture);
        await Assert.That(exception.Message).Contains(BpmnActivityBindingFormat.BindingElementName, StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("An unbound task the document binds resolves to the declared activity, inputs intact")]
    public async Task UnboundTask_WithADeclarationResolves()
    {
        var scope = Bind(
            Definition(BoundElement("approve", BpmnElementTypes.UserTask, new WriteLine("approved"))),
            Unbound("approve"));

        var work = WorkOf(scope, "approve");
        await Assert.That(work).IsOfType(typeof(WriteLine));
        var writeLine = (WriteLine)work;
        await Assert.That(ValueOf<string>(writeLine.Text)).IsEqualTo("approved");
    }

    [Test]
    [DisplayName("An activity binding on an element with no unbound task to bind is refused")]
    public async Task DeadDeclaration_IsRefused()
    {
        // Six of the seven kinds never consult a declaration, so one written on a timer configures nothing. Ignoring
        // it leaves the author's expression sitting in the file while the activity they configured never executes.
        var element = new BpmnElement(
            "wait",
            BpmnElementTypes.IntermediateCatchEvent,
            bindingRef: Ref("wait"),
            extensions: BpmnActivityBindingFormat.Attach(null, Format.Write(new WriteLine("never runs"))));

        var exception = Assert.ThrowsExactly<BpmnBindingException>(() => Bind(Definition(element), Timer("wait", "PT1M")));

        await Assert.That(exception.Message).Contains("wait", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Two scopes declaring the same binding ref get their own activity instance and their own node")]
    public async Task SameBindingRefInTwoScopes_ProducesDistinctActivityNodes()
    {
        // A binding ref is unique within a scope, not across scopes. One instance shared between two scopes still
        // builds and still publishes: ActivityVisitor collects activities into a set and skips one it has already
        // seen, so the second scope simply has no child in the identity graph and half the process never runs.
        var body = new BpmnProcessDefinition("sub", Elements: [BoundElement("work", BpmnElementTypes.ServiceTask, new WriteLine("inner"))]);

        var scope = Bind(
            Definition(BoundElement("work", BpmnElementTypes.ServiceTask, new WriteLine("outer")), Element("sub", BpmnElementTypes.SubProcess)),
            Unbound("work"),
            new BpmnWorkBinding.NestedProcess(ProcessId, "sub", Ref("sub"), BpmnBindingSlot.Primary, body),
            Unbound("work", processId: "sub"));

        var outer = WorkOf(scope, "work");
        var nestedWork = WorkOf(scope, "sub");
        await Assert.That(nestedWork).IsOfType(typeof(BpmnProcess));
        var inner = WorkOf((BpmnProcess)nestedWork, "work");

        await Assert.That(inner).IsNotSameReferenceAs(outer);
        await Assert.That(inner.Id).IsNotEqualTo(outer.Id);

        // The invariant stated the way Elsa sees it: two nodes, one per logical position.
        var nodes = await IdentityGraphOfAsync(scope);
        var texts = nodes.Select(node => node.Activity).OfType<WriteLine>().Select(activity => ValueOf<string>(activity.Text)).Order().ToList();

        await Assert.That(texts).IsEquivalentTo(new[] { "inner", "outer" }, CollectionOrdering.Matching);
    }

    private BpmnProcess Bind(BpmnProcessDefinition definition, params BpmnWorkBinding[] bindings) => Binder.Bind(definition, bindings);

    private static BpmnProcessDefinition Definition(params BpmnElement[] elements) => new(ProcessId, Elements: elements);

    private static BpmnElement Element(string elementId, string elementType) => new(elementId, elementType, bindingRef: Ref(elementId));

    private static BpmnEventDefinition Compensation(string? activityRef = null) =>
        activityRef is null
            ? new(BpmnEventDefinitionTypes.Compensation)
            : new(BpmnEventDefinitionTypes.Compensation, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.ActivityRef] = activityRef });

    /// <summary>A compensation boundary event, which reaches its handler by association rather than by a sequence flow.</summary>
    private static BpmnElement CompensationBoundary(string elementId, string attachedTo, string handler) =>
        new(elementId,
            BpmnElementTypes.BoundaryEvent,
            attachedToRef: attachedTo,
            eventDefinitions: [Compensation()],
            compensationHandlerElementId: handler);

    /// <summary>An unbound task marked as a compensation handler, carrying the activity binding the document declares for it.</summary>
    private BpmnElement CompensationHandler(string elementId, IActivity activity) =>
        new(elementId,
            BpmnElementTypes.ServiceTask,
            bindingRef: Ref(elementId),
            isForCompensation: true,
            extensions: BpmnActivityBindingFormat.Attach(null, Format.Write(activity)));

    private static BpmnWorkBinding.TimerWait Timer(string elementId, string isoDuration) =>
        new(ProcessId, elementId, Ref(elementId), BpmnBindingSlot.Primary, isoDuration);

    private static BpmnWorkBinding.UnboundTask Unbound(string elementId, string processId = ProcessId) =>
        new(processId, elementId, Ref(elementId), BpmnBindingSlot.Primary, BpmnElementTypes.ServiceTask);

    private static string ListenerRef(string elementId) => $"{Ref(elementId)}-listener";

    /// <summary>The activity the scope maps the given element's work to, resolved the way the host resolves it.</summary>
    private static IActivity WorkOf(BpmnProcess scope, string elementId) => WorkForRef(scope, Ref(elementId));

    private static IActivity WorkForRef(BpmnProcess scope, string bindingRef)
    {
        if (!scope.WorkBindings.TryGetValue(bindingRef, out var activityId))
            Assert.Fail($"The scope maps no work to binding ref '{bindingRef}'.");

        var matches = scope.Activities.Where(activity => activity.Id == activityId).ToList();
        if (matches.Count != 1)
            Assert.Fail($"Expected exactly one activity for binding ref '{bindingRef}', but found {matches.Count}.");

        return matches[0];
    }
}

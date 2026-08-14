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
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;

/// <summary>
/// Turning <c>BpmnWorkBinding</c> declarations into the activities a <c>BpmnProcess</c> scope runs.
/// </summary>
public class BpmnWorkBinderTests(ITestOutputHelper testOutputHelper) : BpmnBindingTestBase(testOutputHelper)
{
    private const string ProcessId = "main";

    [Fact(DisplayName = "A timer wait binds to a Delay of the declared ISO-8601 duration")]
    public void TimerWait_BindsToADelay()
    {
        var scope = Bind(Definition(Element("wait", BpmnElementTypes.IntermediateCatchEvent)), Timer("wait", "PT5M"));

        var delay = Assert.IsType<Delay>(WorkOf(scope, "wait"));

        Assert.Equal(TimeSpan.FromMinutes(5), ValueOf<TimeSpan>(delay.TimeSpan));
    }

    [Fact(DisplayName = "A timer wait declaring something that is not an ISO-8601 duration is refused")]
    public void TimerWait_RefusesADurationElsaCannotWaitFor()
    {
        // A duration quietly defaulted to zero is a timer boundary event that fires the moment it is armed, which
        // cancels the task it guards before that task has done anything.
        var exception = Assert.Throws<BpmnBindingException>(() => Bind(Definition(Element("wait", BpmnElementTypes.IntermediateCatchEvent)), Timer("wait", "5 minutes")));

        Assert.Contains("5 minutes", exception.Message);
    }

    [Fact(DisplayName = "A message wait binds to an Event on the message name")]
    public void MessageWait_BindsToAnEvent()
    {
        var scope = Bind(
            Definition(Element("receive", BpmnElementTypes.ReceiveTask)),
            new BpmnWorkBinding.MessageWait(ProcessId, "receive", Ref("receive"), BpmnBindingSlot.Primary, "OrderPlaced"));

        var @event = Assert.IsType<Event>(WorkOf(scope, "receive"));

        Assert.Equal("OrderPlaced", ValueOf<string>(@event.EventName));

        // A catch inside a scope is not a way into the workflow; only the process's own start events are, and saying
        // so is the enclosing BpmnProcess's decision, not this activity's.
        Assert.False(@event.CanStartWorkflow);
    }

    [Fact(DisplayName = "A signal wait binds to an Event on the signal name")]
    public void SignalWait_BindsToAnEvent()
    {
        var scope = Bind(
            Definition(Element("await", BpmnElementTypes.IntermediateCatchEvent)),
            new BpmnWorkBinding.SignalWait(ProcessId, "await", Ref("await"), BpmnBindingSlot.Primary, "Cancelled"));

        Assert.Equal("Cancelled", ValueOf<string>(Assert.IsType<Event>(WorkOf(scope, "await")).EventName));
    }

    [Fact(DisplayName = "A message publish binds to a PublishEvent on the message name")]
    public void MessagePublish_BindsToAPublishEvent()
    {
        var scope = Bind(
            Definition(Element("send", BpmnElementTypes.SendTask)),
            new BpmnWorkBinding.MessagePublish(ProcessId, "send", Ref("send"), BpmnBindingSlot.Primary, "OrderShipped"));

        Assert.Equal("OrderShipped", ValueOf<string>(Assert.IsType<PublishEvent>(WorkOf(scope, "send")).EventName));
    }

    [Fact(DisplayName = "A call activity binds to a DispatchWorkflow on the called element, keeping fire-and-forget")]
    public void CallProcess_BindsToADispatchWorkflow()
    {
        var scope = Bind(
            Definition(Element("call", BpmnElementTypes.CallActivity)),
            new BpmnWorkBinding.CallProcess(ProcessId, "call", Ref("call"), BpmnBindingSlot.Primary, "shipping-process", false));

        var dispatch = Assert.IsType<DispatchWorkflow>(WorkOf(scope, "call"));

        Assert.Equal("shipping-process", ValueOf<string>(dispatch.WorkflowDefinitionId));

        // BPMN has no standard way to say "fire and forget", so the library carries it on the binding. Dropping it
        // here turns an asynchronous call into one the enclosing scope waits on, and the process stops where it used
        // to carry on.
        Assert.False(ValueOf<bool>(dispatch.WaitForCompletion));
    }

    [Fact(DisplayName = "A call activity naming no called element is refused")]
    public void CallProcess_RefusesACallWithNothingToCall()
    {
        Assert.Throws<BpmnBindingException>(() => Bind(
            Definition(Element("call", BpmnElementTypes.CallActivity)),
            new BpmnWorkBinding.CallProcess(ProcessId, "call", Ref("call"), BpmnBindingSlot.Primary, null, true)));
    }

    [Fact(DisplayName = "A nested process binds to a BpmnProcess scope that binds its own work")]
    public void NestedProcess_BindsToANestedScope()
    {
        var body = new BpmnProcessDefinition("sub", Elements: [BoundElement("subWork", BpmnElementTypes.ServiceTask, new WriteLine("nested"))]);

        var scope = Bind(
            Definition(Element("sub", BpmnElementTypes.SubProcess)),
            new BpmnWorkBinding.NestedProcess(ProcessId, "sub", Ref("sub"), BpmnBindingSlot.Primary, body),
            Unbound("subWork", processId: "sub"));

        var nested = Assert.IsType<BpmnProcess>(WorkOf(scope, "sub"));

        Assert.Same(body, nested.Process);
        Assert.Equal("nested", ValueOf<string>(Assert.IsType<WriteLine>(WorkOf(nested, "subWork")).Text));

        // A nested scope's start events are internal to the process around it. The command applier refuses a nested
        // scope that says otherwise, so a binder that marked one would only fail once the process ran.
        Assert.False(nested.IsRootScope);
    }

    [Fact(DisplayName = "A document-declared variable is copied onto the bound scope, and drives a collection-mode multi-instance")]
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

        Assert.Contains(scope.Variables, variable => variable.Name == collectionVariableName);

        var eachActivityId = scope.WorkBindings[Ref("each")];
        var afterActivityId = scope.WorkBindings[Ref("after")];

        var result = await Services.GetRequiredService<IWorkflowRunner>().RunAsync(scope);

        Assert.Equal(3, result.Journal.ActivityExecutionContexts.Count(context => context.Activity.Id == eachActivityId));
        Assert.Equal(1, result.Journal.ActivityExecutionContexts.Count(context => context.Activity.Id == afterActivityId));
        Assert.Empty(result.WorkflowState.Incidents);
    }

    [Fact(DisplayName = "A ScopeListener binding is bound like any other, under its own binding ref")]
    public void ScopeListenerSlot_BindsWithNoSpecialCase()
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

        Assert.IsType<BpmnProcess>(WorkForRef(scope, Ref("escalationHandler")));
        Assert.Equal("Escalated", ValueOf<string>(Assert.IsType<Event>(WorkForRef(scope, ListenerRef("escalationHandler"))).EventName));
    }

    [Fact(DisplayName = "An unbound task the document does not bind is refused, naming the element")]
    public void UnboundTask_WithNoDeclarationIsRefused()
    {
        // The failure that has to be loud. Skipping the binding instead would produce a scope whose interpreter
        // schedules work for a binding ref nothing maps, which surfaces much later and much further away.
        var exception = Assert.Throws<BpmnBindingException>(() => Bind(Definition(Element("approve", BpmnElementTypes.UserTask)), Unbound("approve")));

        Assert.Contains("approve", exception.Message);
        Assert.Contains(BpmnActivityBindingFormat.BindingElementName, exception.Message);
    }

    [Fact(DisplayName = "An unbound task the document binds resolves to the declared activity, inputs intact")]
    public void UnboundTask_WithADeclarationResolves()
    {
        var scope = Bind(
            Definition(BoundElement("approve", BpmnElementTypes.UserTask, new WriteLine("approved"))),
            Unbound("approve"));

        Assert.Equal("approved", ValueOf<string>(Assert.IsType<WriteLine>(WorkOf(scope, "approve")).Text));
    }

    [Fact(DisplayName = "An activity binding on an element with no unbound task to bind is refused")]
    public void DeadDeclaration_IsRefused()
    {
        // Six of the seven kinds never consult a declaration, so one written on a timer configures nothing. Ignoring
        // it leaves the author's expression sitting in the file while the activity they configured never executes.
        var element = new BpmnElement(
            "wait",
            BpmnElementTypes.IntermediateCatchEvent,
            bindingRef: Ref("wait"),
            extensions: BpmnActivityBindingFormat.Attach(null, Format.Write(new WriteLine("never runs"))));

        var exception = Assert.Throws<BpmnBindingException>(() => Bind(Definition(element), Timer("wait", "PT1M")));

        Assert.Contains("wait", exception.Message);
    }

    [Fact(DisplayName = "Two scopes declaring the same binding ref get their own activity instance and their own node")]
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
        var inner = WorkOf(Assert.IsType<BpmnProcess>(WorkOf(scope, "sub")), "work");

        Assert.NotSame(outer, inner);
        Assert.NotEqual(outer.Id, inner.Id);

        // The invariant stated the way Elsa sees it: two nodes, one per logical position.
        var nodes = await IdentityGraphOfAsync(scope);
        var texts = nodes.Select(node => node.Activity).OfType<WriteLine>().Select(activity => ValueOf<string>(activity.Text)).Order().ToList();

        Assert.Equal(new[] { "inner", "outer" }, texts);
    }

    private BpmnProcess Bind(BpmnProcessDefinition definition, params BpmnWorkBinding[] bindings) => Binder.Bind(definition, bindings);

    private static BpmnProcessDefinition Definition(params BpmnElement[] elements) => new(ProcessId, Elements: elements);

    private static BpmnElement Element(string elementId, string elementType) => new(elementId, elementType, bindingRef: Ref(elementId));

    private static BpmnWorkBinding.TimerWait Timer(string elementId, string isoDuration) =>
        new(ProcessId, elementId, Ref(elementId), BpmnBindingSlot.Primary, isoDuration);

    private static BpmnWorkBinding.UnboundTask Unbound(string elementId, string processId = ProcessId) =>
        new(processId, elementId, Ref(elementId), BpmnBindingSlot.Primary, BpmnElementTypes.ServiceTask);

    private static string ListenerRef(string elementId) => $"{Ref(elementId)}-listener";

    /// <summary>The activity the scope maps the given element's work to, resolved the way the host resolves it.</summary>
    private static IActivity WorkOf(BpmnProcess scope, string elementId) => WorkForRef(scope, Ref(elementId));

    private static IActivity WorkForRef(BpmnProcess scope, string bindingRef)
    {
        Assert.True(scope.WorkBindings.TryGetValue(bindingRef, out var activityId), $"The scope maps no work to binding ref '{bindingRef}'.");
        return Assert.Single(scope.Activities, activity => activity.Id == activityId);
    }
}

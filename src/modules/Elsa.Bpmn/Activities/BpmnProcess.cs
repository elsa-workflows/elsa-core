using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Bpmn.Model;
using Elsa.Bpmn.Hosting;
using Elsa.Bpmn.Signals;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Signals;

namespace Elsa.Bpmn.Activities;

/// <summary>
/// Runs one BPMN process scope, driving the <c>Bpmn.Semantics</c> interpreter and applying what it returns onto this
/// activity's execution context.
/// </summary>
/// <remarks>
/// <para>
/// A scope owns its own execution state and its own record of the work it started, both held in
/// <see cref="ActivityExecutionContext.Properties"/>. A nested BPMN scope — an embedded subprocess, or an event
/// subprocess body — is another <see cref="BpmnProcess"/> bound as work, so the scope hierarchy the interpreter has
/// no view of is exactly the activity hierarchy Elsa already maintains.
/// </para>
/// <para>
/// Like every container, this one never auto-completes: it completes when the interpreter returns a <c>Complete</c>
/// continuation, and its outcome is what a conditional sequence flow in the enclosing scope selects on.
/// </para>
/// <para>
/// Composing this activity into a <c>Flowchart</c>: it completes with only the interpreter's outcome name (e.g.
/// <c>BpmnInterpreter.DoneOutcomeName</c>, and <c>CancelledOutcomeName</c> where relevant) — not with
/// <c>Outcomes.Default</c>, which an ordinary activity's null result also produces and which additionally matches a
/// null-port connection. A <c>Connection</c> built with the default/null-port shorthand will therefore never fire
/// from this activity; always target an explicit outcome port.
/// </para>
/// </remarks>
[Activity("Elsa", "BPMN", "Executes a BPMN process scope.")]
[System.ComponentModel.Browsable(false)]
public class BpmnProcess : Container
{
    /// <inheritdoc />
    public BpmnProcess([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        OnSignalReceived<BpmnScopeSignal>(OnScopeSignalledAsync);
        OnSignalReceived<FaultSignal>(OnWorkFaultedAsync);
    }

    /// <summary>
    /// The BPMN process definition this scope executes.
    /// </summary>
    public BpmnProcessDefinition? Process { get; set; }

    /// <summary>
    /// Whether this scope is the root BPMN process of its workflow, and may therefore register the start triggers its
    /// definition declares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Off unless something says otherwise, which is the answer every nested scope needs: the start events of a
    /// subprocess body, of an event subprocess body, and of a process composed into a <c>Flowchart</c> are internal
    /// to the graph around them, not ways into the workflow. Root position cannot be recovered from a published
    /// activity node — a node knows neither its parent nor how it was imported — so whoever builds the graph says so
    /// explicitly and everything that nests a scope leaves it alone.
    /// </para>
    /// <para>
    /// Backed by Elsa's own <see cref="Activity.CanStartWorkflow"/> rather than by a second flag, because
    /// <c>TriggerIndexer</c> gates registration on that one: two flags could disagree, and the disagreement would
    /// show up as a subprocess quietly registered as an entry point. Reading it here gives the BPMN meaning a name
    /// and one place to document it. Trigger registration itself belongs to the issue that makes this activity an
    /// <c>ITrigger</c>; this property is what that gate will read.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public bool IsRootScope
    {
        get => CanStartWorkflow;
        set => CanStartWorkflow = value;
    }

    /// <summary>
    /// Maps each binding ref the definition declares to the id of the activity in <see cref="Container.Activities"/>
    /// that runs it.
    /// </summary>
    /// <remarks>
    /// The interpreter never parses a binding ref — it compares and echoes it — so resolving one to an actual timer,
    /// work item, HTTP call or nested process is entirely the host's.
    /// </remarks>
    public IDictionary<string, string> WorkBindings { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <inheritdoc />
    protected override ValueTask ScheduleChildrenAsync(ActivityExecutionContext context) => BpmnScopeHost.For(context).StartAsync();

    /// <summary>
    /// The activity bound to the given binding ref, or <c>null</c> when the definition declares a binding this
    /// activity does not map.
    /// </summary>
    internal IActivity? FindWorkActivity(string bindingRef) =>
        WorkBindings.TryGetValue(bindingRef, out var activityId)
            ? Activities.FirstOrDefault(activity => string.Equals(activity.Id, activityId, StringComparison.Ordinal))
            : null;

    /// <summary>
    /// A unit of work completed. Named rather than a lambda, because completion callbacks are rehydrated by method name.
    /// </summary>
    internal ValueTask OnWorkCompletedAsync(ActivityCompletedContext context) =>
        BpmnScopeHost.For(context.TargetContext).OnWorkCompletedAsync(context.ChildContext, context.Result);

    private ValueTask OnScopeSignalledAsync(BpmnScopeSignal signal, SignalContext context) =>
        BpmnScopeHost.For(context.ReceiverActivityExecutionContext).OnScopeSignalledAsync(signal, context);

    private ValueTask OnWorkFaultedAsync(FaultSignal signal, SignalContext context) =>
        BpmnScopeHost.For(context.ReceiverActivityExecutionContext).OnWorkFaultedAsync(signal, context);
}

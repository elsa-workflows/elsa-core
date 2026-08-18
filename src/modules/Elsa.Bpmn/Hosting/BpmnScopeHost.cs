using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Exceptions;
using Elsa.Bpmn.Signals;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Signals;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// The host side of the <c>Bpmn.Semantics</c> port for one BPMN scope: it feeds the interpreter's four entry points
/// and applies what comes back onto the scope's <see cref="ActivityExecutionContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every entry point is synchronous and returns a value; the interpreter never calls back. The host's job is to say
/// what its world looks like — a snapshot — and then to do what it is told, in the order it is told.
/// </para>
/// <para>
/// A host instance is a view over one scope's context and is created per call. Everything durable lives in
/// <see cref="BpmnScopeMemory"/>, everything derived lives in the context's transient properties, and everything
/// ordering-related lives in the instance-wide <see cref="BpmnScopeDispatcher"/>.
/// </para>
/// </remarks>
internal sealed class BpmnScopeHost
{
    /// <summary>
    /// What this host promises it can do.
    /// </summary>
    /// <remarks>
    /// Each capability is a claim, honoured elsewhere in this module: subtree cancellation by
    /// <see cref="BpmnWorkTeardown"/>, scope signalling by <see cref="BpmnScopeSignal"/>, iteration scopes by the
    /// applier's per-instance variables, and <see cref="BpmnHostCapabilities.ScopeVariables"/> by
    /// <see cref="BpmnScopeVariables"/>. Spelled out rather than written as <c>BpmnHostCapabilities.Full</c> on
    /// purpose: <c>Full</c> would silently grow to include a capability a later library version adds and this host
    /// has never implemented, which is the one way a declaration can become a lie without anyone editing it.
    /// The same set goes to <see cref="BpmnGraph.Build"/> and to every snapshot, which the port requires.
    /// </remarks>
    public const BpmnHostCapabilities Capabilities =
        BpmnHostCapabilities.SubtreeCancellation | BpmnHostCapabilities.ScopeSignalling | BpmnHostCapabilities.IterationScopes | BpmnHostCapabilities.ScopeVariables;

    /// <summary>
    /// The property key under which a nested scope's invocation correlation is carried on its own context.
    /// </summary>
    public const string InvocationCorrelationPropertyKey = "Bpmn:InvocationCorrelation";

    private const string GraphTransientPropertyKey = "Bpmn:Graph";

    // The interpreter is a pure function of its request: it holds no per-instance state, so one instance serves the
    // whole process. Creating one per evaluation would only re-register the built-in element behaviors.
    private static readonly BpmnInterpreter Interpreter = BpmnInterpreter.CreateDefault();

    private static readonly object DispatcherKey = new();
    private static readonly IReadOnlyDictionary<string, string> NoCorrelation = new Dictionary<string, string>(StringComparer.Ordinal);

    private readonly ActivityExecutionContext _context;
    private readonly BpmnProcess _process;

    private BpmnScopeHost(ActivityExecutionContext context)
    {
        _context = context;
        _process = (BpmnProcess)context.Activity;
    }

    /// <summary>Returns the host for the given BPMN scope context.</summary>
    public static BpmnScopeHost For(ActivityExecutionContext context) => new(context);

    /// <summary>The evaluation queue shared by every BPMN scope in this workflow instance.</summary>
    public static BpmnScopeDispatcher DispatcherOf(WorkflowExecutionContext context) =>
        context.TransientProperties.GetOrAdd(DispatcherKey, () => new BpmnScopeDispatcher());

    /// <summary>The built graph for this scope. Derived from the definition, the bound work and the capabilities, none of which vary per instance.</summary>
    public BpmnGraph Graph => _context.TransientProperties.GetOrAdd(GraphTransientPropertyKey, BuildGraph);

    // --- The interpreter's four entry points ---------------------------------------------------------

    /// <summary>The scope is beginning.</summary>
    public ValueTask StartAsync() => EvaluateAsync(memory =>
        Interpreter.Start(new BpmnStartRequest(Graph, memory.State, Snapshot(memory))));

    /// <summary>A unit of work finished, reporting zero or more outcome names.</summary>
    public ValueTask OnWorkCompletedAsync(ActivityExecutionContext childContext, object? result) => EvaluateAsync(memory =>
    {
        // Keyed on the child's own context id. A completion for work this scope no longer holds is absorbed rather
        // than faulted: an interrupting boundary tears its host down while the host's work is still in flight, and a
        // late completion for work that was torn down is an ordinary BPMN race.
        if (memory.Work.FindByChildContextId(childContext.Id) is not { } record)
            return null;

        // The completing work must ALREADY be gone from LiveWork when the interpreter is asked.
        memory.Work.Remove(record);
        memory.SaveWork();

        var outcomeNames = result is Outcomes outcomes ? outcomes.Names : [];

        return Interpreter.OnWorkCompleted(new BpmnWorkCompletedRequest(
            Graph, memory.State, Snapshot(memory), record.BindingRef, record.Handle, outcomeNames, record.IterationId));
    });

    /// <summary>A nested scope this one invoked signalled outward.</summary>
    public ValueTask OnScopeSignalledAsync(BpmnScopeSignal signal, SignalContext signalContext)
    {
        var sender = signalContext.SenderActivityExecutionContext;

        // The channel delivers to the sender before walking its ancestors, and a scope never signals itself.
        if (string.Equals(sender.Id, _context.Id, StringComparison.Ordinal))
            return default;

        // A scope signal is for the immediate enclosing scope, which is the one that started the sender's work. Any
        // other receiver lets it keep bubbling; that is also how an unrelated container in between composes.
        if (BpmnScopeMemory.Load(_context).Work.FindByChildContextId(sender.Id) is not { } signalling)
            return default;

        signalContext.StopPropagation();

        return EvaluateAsync(memory =>
        {
            // Unlike a completion, the signalling work stays in the ledger: an escalating activity keeps running, and
            // removing it makes the interpreter believe it has already gone.
            if (memory.Work.FindByHandle(signalling.Handle) is not { } record)
                return null;

            return Interpreter.OnWorkSignalled(new BpmnWorkSignalledRequest(
                Graph, memory.State, Snapshot(memory), record.BindingRef, record.Handle, signal.Code, signal.Payload, record.IterationId));
        });
    }

    /// <summary>
    /// A unit of work failed. Rides the <see cref="FaultSignal"/> seam: handle the signal, ask the interpreter what
    /// BPMN makes of the fault, and claim it only when a catcher took it.
    /// </summary>
    /// <remarks>
    /// The disposition has to be decided before this handler returns, so the interpreter is asked inline; only the
    /// commands are applied through the dispatcher. A <c>Propagated</c> disposition is left strictly alone — no
    /// <c>StopPropagation</c>, nothing terminalized — so the fault reaches the enclosing scope, which is how BPMN
    /// error propagation crosses a scope boundary, or the incident strategy, which is how it surfaces at the root.
    /// </remarks>
    public async ValueTask OnWorkFaultedAsync(FaultSignal signal, SignalContext signalContext)
    {
        var memory = BpmnScopeMemory.Load(_context);

        if (ResolveFaultedWork(memory, signal.FaultedContext) is not { } record)
            return;

        // As with a completion, the failed work must ALREADY be removed before the interpreter is asked.
        memory.Work.Remove(record);
        memory.SaveWork();

        var evaluation = Interpreter.OnWorkFaulted(new BpmnWorkFaultedRequest(
            Graph, memory.State, Snapshot(memory), record.BindingRef, record.Handle, signal.Exception.Message));

        memory.State = evaluation.State.Prune();
        memory.SaveState();

        if (evaluation.Disposition is not BpmnErrorDisposition.Caught)
            return;

        signalContext.StopPropagation();

        // A handler that claims a fault owns terminalizing the failed activity, and BPMN terminalizes the whole unit of
        // work rather than the one activity that threw: when the failure came from inside a nested scope, this scope's
        // failing work is that scope. Cancelling it recursively covers the activity that actually threw. The interpreter
        // issues no teardown for failed work — it treats it as already terminal — so this is the host's own doing and
        // not a command out of order. RecoverFromFault stays the middleware's alone: it decrements every ancestor's
        // fault count, so a second call drives them negative.
        if (BpmnWorkTeardown.FindContext(_context.WorkflowExecutionContext, record.ChildContextId) is { } failedWorkContext)
            await BpmnWorkTeardown.CancelSubtreeAsync(failedWorkContext, $"element '{record.ElementId}' failed");

        await DispatcherOf(_context.WorkflowExecutionContext).PostAsync(() => ApplyAsync(memory, evaluation));
    }

    // --- Plumbing ------------------------------------------------------------------------------------

    private ValueTask EvaluateAsync(Func<BpmnScopeMemory, BpmnEvaluation?> evaluate) =>
        DispatcherOf(_context.WorkflowExecutionContext).PostAsync(async () =>
        {
            var memory = BpmnScopeMemory.Load(_context);
            var evaluation = evaluate(memory);

            if (evaluation is null)
                return;

            // Persist the state before acting on the commands: a command applied against a state that was never
            // recorded is how a crash produces work with no token behind it.
            memory.State = evaluation.State.Prune();
            memory.SaveState();

            await ApplyAsync(memory, evaluation);
        });

    private async ValueTask ApplyAsync(BpmnScopeMemory memory, BpmnEvaluation evaluation)
    {
        await new BpmnCommandApplier(_context, _process, memory).ApplyAsync(evaluation.Commands);

        switch (evaluation.Continuation)
        {
            case BpmnContinuation.Complete complete:
                // The scope completes because the interpreter said so, never because it ran out of children.
                await _context.CompleteActivityAsync(new Outcomes(complete.Outcome));
                break;
            case BpmnContinuation.Defer:
                break;
            case BpmnContinuation.Fault fault:
                throw new BpmnScopeFaultException(fault.Code, fault.Message);
            default:
                throw new NotSupportedException($"The BPMN continuation '{evaluation.Continuation.GetType().Name}' is not supported by this host.");
        }
    }

    /// <summary>
    /// Finds the unit of work this scope started that the failing activity belongs to, walking outward from the
    /// failure.
    /// </summary>
    /// <remarks>
    /// A fault raised deep inside a nested scope is, to this scope, its own subprocess work failing. The nested scope
    /// sees the signal first and claims it if it has a catcher; if it does not, the signal arrives here and this walk
    /// is what turns "some activity failed" into "the work I started failed", which is exactly what BPMN error
    /// propagation across a scope boundary means.
    /// </remarks>
    private BpmnWorkRecord? ResolveFaultedWork(BpmnScopeMemory memory, ActivityExecutionContext faultedContext)
    {
        for (var current = faultedContext; current is not null && !string.Equals(current.Id, _context.Id, StringComparison.Ordinal); current = current.ParentActivityExecutionContext)
        {
            if (memory.Work.FindByChildContextId(current.Id) is { } record)
                return record;
        }

        return null;
    }

    private BpmnHostSnapshot Snapshot(BpmnScopeMemory memory)
    {
        var invocationCorrelation = InvocationCorrelation;

        return new BpmnHostSnapshot(
            ScopeInstanceId: _context.Id,
            // A scope has an enclosing one exactly when another scope started it, which is what the carried
            // correlation records. A root process has none, so an unhandled escalation is a documented no-op.
            HasEnclosingScope: invocationCorrelation.Count > 0,
            LiveWork: memory.Work.ToLiveWork(),
            InvocationCorrelation: invocationCorrelation,
            Variables: new BpmnScopeVariables(_context),
            Capabilities: Capabilities);
    }

    /// <summary>
    /// The correlation of the work that started this scope. It belongs to the scope and is fixed for its lifetime;
    /// a completing unit of work's correlation is never written here, because the event-subprocess start hint is read
    /// from this same dictionary.
    /// </summary>
    private IReadOnlyDictionary<string, string> InvocationCorrelation =>
        BpmnScopeMemory.Read<Dictionary<string, string>>(_context, InvocationCorrelationPropertyKey) ?? NoCorrelation;

    private BpmnGraph BuildGraph()
    {
        var definition = _process.Process
                         ?? throw new InvalidOperationException($"BPMN process activity '{_process.Id}' has no process definition to execute.");

        // Every binding the definition declares, with the nested definition attached where the bound activity is
        // itself a BPMN scope. The graph validator reads that for an event subprocess body's start trigger.
        var boundWork = BpmnBoundWork.Derive(definition, bindingRef => (_process.FindWorkActivity(bindingRef) as BpmnProcess)?.Process);

        return BpmnGraph.Build(definition, boundWork, Capabilities);
    }
}

using System.Text.Json;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Signals;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Options;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Translates the interpreter's three host commands onto <see cref="ActivityExecutionContext"/>.
/// </summary>
/// <remarks>
/// <list type="table">
///   <item><term><c>StartWork</c></term><description><see cref="ActivityExecutionContext.ScheduleActivityAsync(IActivity?, ScheduleWorkOptions?)"/>.</description></item>
///   <item><term><c>CancelWorkSubtree</c></term><description><c>CancelActivityAsync</c>, which already walks the child subtree recursively and withdraws its not-yet-invoked work.</description></item>
///   <item><term><c>SignalEnclosingScope</c></term><description><c>SendSignalAsync</c>, which bubbles to ancestors.</description></item>
/// </list>
/// </remarks>
internal sealed class BpmnCommandApplier(ActivityExecutionContext scopeContext, BpmnProcess process, BpmnScopeMemory memory)
{
    /// <summary>
    /// Applies a command list <b>in the order returned</b>.
    /// </summary>
    /// <remarks>
    /// The ordering carries meaning and is not an implementation detail. An interrupting boundary event emits the
    /// boundary path's <c>StartWork</c> <i>before</i> the teardown that retires the host it interrupted, and a host
    /// that tidied up first would be applying a different process.
    /// </remarks>
    public async ValueTask ApplyAsync(IReadOnlyList<BpmnHostCommand> commands)
    {
        // Refused before anything in the batch is applied. Applying commands one at a time and refusing only once
        // the offending StartWork is reached would leave earlier commands in the same batch already applied and
        // saved via memory.SaveWork() below — and under ContinueWithIncidentsStrategy that throw is absorbed into
        // an incident rather than surfaced, so the workflow would carry on with a half-applied batch and half-saved
        // memory instead of the refusal stopping it clean.
        foreach (var start in commands.OfType<BpmnHostCommand.StartWork>())
        {
            if (process.FindWorkActivity(start.BindingRef) is BpmnProcess { IsRootScope: true } nested)
            {
                throw new InvalidOperationException(
                    $"BPMN element '{start.ElementId}' binds process activity '{nested.Id}' as the work of scope '{process.Id}', but that activity declares itself the workflow's root scope. "
                    + "A nested scope's start events are internal to the process around it, not workflow entry points, so it must not be marked as able to start the workflow.");
            }
        }

        foreach (var command in commands)
        {
            switch (command)
            {
                case BpmnHostCommand.StartWork start:
                    await StartWorkAsync(start);
                    break;
                case BpmnHostCommand.CancelWorkSubtree cancel:
                    await CancelWorkSubtreeAsync(cancel);
                    break;
                case BpmnHostCommand.SignalEnclosingScope signal:
                    await SignalEnclosingScopeAsync(signal);
                    break;
                default:
                    // The command hierarchy is closed, so this can only be reached by a library version that added a
                    // command this host has never heard of. Refusing is the only honest answer: silently skipping it
                    // would run a different process than the one the interpreter decided on.
                    throw new NotSupportedException($"The BPMN host command '{command.GetType().Name}' is not supported by this host.");
            }

            memory.SaveWork();
        }
    }

    private async ValueTask StartWorkAsync(BpmnHostCommand.StartWork start)
    {
        var activity = process.FindWorkActivity(start.BindingRef)
                       ?? throw new InvalidOperationException(
                           $"BPMN element '{start.ElementId}' binds work '{start.BindingRef}', which activity '{process.Id}' does not map to a child activity.");

        // The rule that a nested scope registers no start triggers is enforced by ApplyAsync's pre-scan, before any
        // command in the batch is applied — not here, where earlier commands in the same batch could already have
        // been applied and persisted.
        var workflowExecutionContext = scopeContext.WorkflowExecutionContext;

        // The child's context is created up front so that this scope has its id before the child ever runs, and can
        // key the unit of work on it. The alternative — recognising the child by ActivityExecutionContext.Tag — is
        // unsound across nested scopes, because the completion-callback dispatch rewrites the receiving context's Tag.
        var childContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(activity, new ActivityInvocationOptions
        {
            Owner = scopeContext,
            Variables = BuildIterationVariables(start.IterationScope),
            SchedulingActivityExecutionId = scopeContext.Id
        });

        // The correlation is opaque interpreter state that must travel with the work and, when the work is a nested
        // BPMN process, arrive there as its InvocationCorrelation. It goes on the child's own context rather than on
        // the activity instance: an activity object is shared by every concurrent execution of one definition, so
        // writing per-invocation state onto it corrupts as soon as two instances run at once.
        BpmnScopeMemory.Write(childContext, BpmnScopeHost.InvocationCorrelationPropertyKey, start.Correlation);

        childContext.Taint();
        workflowExecutionContext.AddActivityExecutionContext(childContext);

        // Recorded before scheduling, so the work is live from the moment anything could report against it. This
        // never checks for an existing record on the same (BindingRef, IterationId): the port guarantees the
        // interpreter never issues a second StartWork for a slot it already holds live, so a duplicate here would be
        // an interpreter contract breach, not a host-side race. Were it to happen anyway, Records is append-only and
        // keyed by handle rather than by slot, so nothing gets overwritten or stranded — the older record, and the
        // context behind it, stay exactly as reachable as before. What would go wrong is the snapshot then reporting
        // two live entries for one slot, which is the interpreter's invariant to keep, not this host's to enforce.
        memory.Work.Records.Add(new BpmnWorkRecord
        {
            Handle = memory.Work.NextHandle(),
            BindingRef = start.BindingRef,
            IterationId = start.IterationScope?.IterationId,
            ElementId = start.ElementId,
            ChildContextId = childContext.Id
        });

        // A named instance method, not a lambda: completion callbacks are rehydrated by method name.
        await scopeContext.ScheduleActivityAsync(activity, new ScheduleWorkOptions
        {
            CompletionCallback = process.OnWorkCompletedAsync,
            ExistingActivityExecutionContext = childContext,
            SchedulingActivityExecutionId = scopeContext.Id
        });
    }

    private async ValueTask CancelWorkSubtreeAsync(BpmnHostCommand.CancelWorkSubtree cancel)
    {
        // The interpreter only ever names a handle this scope reported as live, so a miss means the work has already
        // gone — a race BPMN produces routinely, and one the interpreter absorbs on the way back in.
        if (memory.Work.FindByHandle(cancel.Handle) is not { } record)
            return;

        memory.Work.Remove(record);

        // Saved here rather than left to the end-of-command save in ApplyAsync: cancelling the subtree runs arbitrary
        // activity code (CancelSignal handlers, cancellation notifications), any of which can throw, and under the
        // continue-with-incidents strategy such a throw is absorbed into an incident rather than left to crash the
        // process — so the end-of-command save would never run. Saving the removal now, before the teardown, keeps
        // the persisted ledger from claiming work this scope just tore down, and is what lets a completion callback
        // that arrives for that work anyway find no live record and be discarded (see
        // BpmnScopeHost.OnWorkCompletedAsync) instead of being handed to the interpreter as real work.
        memory.SaveWork();

        if (BpmnWorkTeardown.FindContext(scopeContext.WorkflowExecutionContext, record.ChildContextId) is not { } childContext)
            return;

        // On the scope-completion path (an armed listener retired when its scope completes), this call is measured
        // redundant with Elsa's own CompleteActivityAsync, which already cancels a completed container's
        // non-completed children: see BpmnEventSubprocessTests.MessageEventSubprocess_RetiresTheStillArmedListenerWhenTheScopeCompletes,
        // where every assertion but the ledger removal above still holds with this call skipped. The ledger removal
        // is this host's own contribution and is not redundant. The fault-teardown path (BpmnScopeHost, tearing down
        // a claimed fault's sibling work) is a different call site and was not part of that measurement.
        await BpmnWorkTeardown.CancelSubtreeAsync(childContext, $"element '{cancel.ElementId}', {cancel.Reason}");
    }

    private ValueTask SignalEnclosingScopeAsync(BpmnHostCommand.SignalEnclosingScope signal) =>
        scopeContext.SendSignalAsync(new BpmnScopeSignal(signal.Code, signal.Payload));

    private static ICollection<Variable>? BuildIterationVariables(BpmnIterationScope? iterationScope) =>
        iterationScope?.Values.Select(value => new Variable(value.Key, ToClrValue(value.Value))).ToList();

    private static object? ToClrValue(BpmnValue value) => value.Json is not { } json
        ? null
        : json.ValueKind switch
        {
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Number => json.TryGetInt64(out var integer) ? integer : json.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => json.GetRawText()
        };
}

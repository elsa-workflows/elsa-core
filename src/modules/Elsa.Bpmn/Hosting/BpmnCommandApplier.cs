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
///   <item><term><c>CancelWorkSubtree</c></term><description><c>CancelActivityAsync</c>, which already walks the child subtree recursively.</description></item>
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

        // Recorded before scheduling, so the work is live from the moment anything could report against it.
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

        if (BpmnWorkTeardown.FindContext(scopeContext.WorkflowExecutionContext, record.ChildContextId) is not { } childContext)
            return;

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

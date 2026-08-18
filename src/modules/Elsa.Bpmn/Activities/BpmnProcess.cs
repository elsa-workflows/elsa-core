using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml;
using Bpmn.Model;
using Elsa.Bpmn.Hosting;
using Elsa.Bpmn.Signals;
using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Signals;
using Microsoft.Extensions.Logging;

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
public class BpmnProcess : Container, ITrigger
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
    /// and one place to document it. This is the gate <see cref="Elsa.Workflows.Runtime.TriggerIndexer"/> reads
    /// before it ever asks this activity for trigger payloads — see <see cref="GetStartTriggerPayloadsAsync"/>,
    /// which re-checks nesting from the graph itself because this flag alone is not enough once a scope can be
    /// composed deeper after it is set.
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

    /// <inheritdoc />
    ValueTask<IEnumerable<object>> ITrigger.GetTriggerPayloadsAsync(TriggerIndexingContext context) => GetStartTriggerPayloadsAsync(context);

    /// <summary>
    /// Walks this process's own event-defined start events and returns one bookmark datum per resolvable message or
    /// signal name, and per recurring timer start. <see cref="Elsa.Workflows.Runtime.TriggerIndexer"/> only ever calls this for an activity
    /// whose <see cref="Activity.CanStartWorkflow"/> already reads <c>true</c> — see <see cref="IsRootScope"/> — but
    /// that flag is set once, by whoever last composed this scope, and cannot see composition that happens later.
    /// <see cref="HasEnclosingBpmnScopeAsync"/> re-derives entry-point status from the graph itself so a scope that
    /// is genuinely nested — directly, or via an intermediate <c>Flowchart</c> — never registers a trigger no matter
    /// what the flag says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only <c>messageEventDefinition</c> and <c>signalEventDefinition</c> start events resolve to a payload here,
    /// and both resolve to the same <see cref="Elsa.Workflows.Runtime.Stimuli.EventStimulus"/> that
    /// <c>Event</c>/<c>PublishEvent</c> already key their own bookmarks on, keyed on the resolved name alone — the
    /// library correlates a message and a signal start the same way, so reusing the stimulus type an external
    /// publisher already speaks is what makes a <c>.bpmn</c> file portable rather than BPMN-specific.
    /// </para>
    /// <para>
    /// A <c>timerEventDefinition</c> start resolves only when it is a recurring schedule: an ISO-8601
    /// <c>&lt;timeCycle&gt;</c> interval registers through the same <see cref="TimerTriggerPayload"/>/
    /// <see cref="SchedulingStimulusNames.Timer"/> path <c>Elsa.Scheduling</c>'s own <c>Timer</c> activity uses, and a
    /// cron cycle through <see cref="CronTriggerPayload"/>/<see cref="SchedulingStimulusNames.Cron"/>. A one-shot
    /// <c>&lt;timeDate&gt;</c>/<c>&lt;timeDuration&gt;</c> start, and any other event definition, is not represented
    /// here at all: the reader already degraded it to a plain start event at import (see
    /// <c>BpmnActivityBindingFormat</c>'s sibling, the interchange reader), so there is nothing left to resolve.
    /// </para>
    /// <para>
    /// Each payload is wrapped in a <see cref="NamedTriggerPayload"/> naming its own stimulus, rather than relying on
    /// <see cref="TriggerIndexingContext.TriggerName"/>: that property is a single value shared by every payload of
    /// the trigger, so a process with both a message/signal start and a recurring timer start would otherwise have
    /// the last kind processed claim the name for every row, storing the earlier rows under a hash no publisher
    /// would ever compute.
    /// </para>
    /// <para>
    /// Two start events (or two event definitions on one start event) that resolve to the same stimulus name and
    /// value are collapsed to a single payload: <c>Elsa.Workflows.Runtime.StimulusSender</c> starts the workflow
    /// once per matched <see cref="Elsa.Workflows.Runtime.Entities.StoredTrigger"/> row, so two identical rows would
    /// start the workflow twice for one inbound stimulus. Distinct resolved names, and distinct stimulus kinds, are
    /// never collapsed into each other.
    /// </para>
    /// <para>
    /// A malformed <c>&lt;timeCycle&gt;</c> interval is refused for its own start event only: the offending element
    /// is logged and skipped, and every other valid start event on this process still registers. Letting the
    /// exception propagate would not make the failure any louder — <c>TriggerIndexer.TryGetTriggerDataAsync</c>
    /// catches around the whole <see cref="ITrigger.GetTriggerPayloadsAsync"/> call and only logs a warning, so an
    /// unhandled exception here would silently discard every other start on the process, which is the defect this
    /// method exists to close.
    /// </para>
    /// </remarks>
    private async ValueTask<IEnumerable<object>> GetStartTriggerPayloadsAsync(TriggerIndexingContext context)
    {
        if (Process is not { } process)
            return [];

        if (await HasEnclosingBpmnScopeAsync(context))
            return [];

        var logger = context.ExpressionExecutionContext.GetRequiredService<ILogger<BpmnProcess>>();
        var payloads = new List<object>();
        var registeredStimuli = new HashSet<(string StimulusName, string ResolvedName)>();

        foreach (var element in process.Elements)
        {
            if (!string.Equals(element.ElementType, BpmnElementTypes.StartEvent, StringComparison.Ordinal))
                continue;

            foreach (var eventDefinition in element.EventDefinitions)
                AddStartTriggerPayload(context, element, eventDefinition, registeredStimuli, payloads, logger);
        }

        return payloads;
    }

    private static void AddStartTriggerPayload(
        TriggerIndexingContext context,
        BpmnElement element,
        BpmnEventDefinition eventDefinition,
        ISet<(string StimulusName, string ResolvedName)> registeredStimuli,
        ICollection<object> payloads,
        ILogger logger)
    {
        switch (eventDefinition.Type)
        {
            case BpmnEventDefinitionTypes.Message:
            case BpmnEventDefinitionTypes.Signal:
                if (eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Name, out var name) && !string.IsNullOrWhiteSpace(name))
                {
                    if (registeredStimuli.Add((RuntimeStimulusNames.Event, name)))
                        payloads.Add(new NamedTriggerPayload(RuntimeStimulusNames.Event, context.GetEventStimulus(name)));
                }
                break;

            case BpmnEventDefinitionTypes.Timer:
                if (eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Interval, out var isoInterval))
                {
                    TimeSpan interval;

                    try
                    {
                        interval = XmlConvert.ToTimeSpan(isoInterval);
                    }
                    catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentNullException)
                    {
                        logger.LogWarning(
                            exception,
                            "BPMN element '{ElementId}' declares the timer duration '{IsoInterval}', which is not an ISO-8601 duration Elsa can wait for. "
                            + "Skipping this start event; the process's other start events still register.",
                            element.ElementId,
                            isoInterval);
                        break;
                    }

                    if (registeredStimuli.Add((SchedulingStimulusNames.Timer, interval.ToString())))
                        payloads.Add(new NamedTriggerPayload(SchedulingStimulusNames.Timer, context.GetTimerTriggerStimulus(interval)));
                }
                else if (eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Cron, out var cron))
                {
                    if (registeredStimuli.Add((SchedulingStimulusNames.Cron, cron)))
                        payloads.Add(new NamedTriggerPayload(SchedulingStimulusNames.Cron, new CronTriggerPayload(cron)));
                }
                break;
        }
    }

    /// <summary>
    /// Whether this scope sits inside another BPMN scope anywhere above it in the published workflow graph —
    /// directly, as a subprocess or event-subprocess body, or indirectly, through an intermediate <c>Flowchart</c>
    /// (D11 makes composing a <c>BpmnProcess</c> into a <c>Flowchart</c> a first-class shape).
    /// </summary>
    /// <remarks>
    /// <see cref="IsRootScope"/> is only ever set, never inferred, by whoever last constructs or composes this
    /// scope. A binder sets it once, on the one top-level scope it produces; nesting it deeper afterwards — for
    /// instance by placing that same activity inside a <c>Flowchart</c> that is itself bound as work under another
    /// <c>BpmnProcess</c> — leaves the flag untouched, because the composer doing the nesting is not the binder and
    /// has no reason to revisit a flag it never set. <see cref="Elsa.Bpmn.Hosting.BpmnCommandApplier"/> already refuses to
    /// schedule a scope bound <i>directly</i> as another scope's work when that scope claims root position, but it
    /// only ever inspects work bound directly to the enclosing <c>BpmnProcess</c>; a scope reached through an
    /// intermediate <c>Flowchart</c> is invisible to that check. Re-deriving the answer from the whole workflow graph
    /// at indexing time — closer to the thing being protected, registration of a trigger nobody asked for — closes
    /// that gap without needing every composer of a <c>BpmnProcess</c> to remember to clear a flag it never set.
    /// </remarks>
    private async ValueTask<bool> HasEnclosingBpmnScopeAsync(TriggerIndexingContext context)
    {
        var activityVisitor = context.ExpressionExecutionContext.GetRequiredService<IActivityVisitor>();
        var root = await activityVisitor.VisitAsync(context.WorkflowIndexingContext.Workflow.Root, context.CancellationToken);
        var node = ReferenceEquals(root.Activity, this) ? root : root.Descendants().FirstOrDefault(descendant => ReferenceEquals(descendant.Activity, this));

        return node is not null && node.Ancestors().Any(ancestor => ancestor.Activity is BpmnProcess);
    }

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

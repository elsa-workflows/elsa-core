using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Stimuli;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.Triggers;

/// <summary>
/// A <see cref="BpmnProcess"/>'s own event-defined start events register as workflow triggers when the scope is the
/// workflow's root -- and register nothing when the scope is nested, directly or through an intermediate
/// <c>Flowchart</c>, no matter what <see cref="BpmnProcess.IsRootScope"/> itself says.
/// </summary>
public class BpmnProcessTriggerTests : IAsyncDisposable
{
    private readonly IServiceProvider _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
        .ConfigureElsa(elsa => elsa.UseBpmn())
        .Build();

    public async ValueTask DisposeAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("A message start event registers exactly one trigger, on the resolved name")]
    public async Task MessageStartEvent_RegistersOneTriggerOnName()
    {
        var process = RootScope("message-start", Message("OrderPlaced"));

        var trigger = (await Assert.That(await IndexAsync(process)).HasSingleItem())!;


        var payloadValue = trigger.Payload;


        await Assert.That(payloadValue).IsOfType(typeof(EventStimulus));


        var payload = (EventStimulus)payloadValue!;
        await Assert.That(payload.EventName).IsEqualTo("OrderPlaced");

    }

    [Test]
    [DisplayName("A signal start event registers exactly one trigger, on the resolved name")]
    public async Task SignalStartEvent_RegistersOneTriggerOnName()
    {
        var process = RootScope("signal-start", Signal("Cancelled"));

        var trigger = (await Assert.That(await IndexAsync(process)).HasSingleItem())!;


        var payloadValue = trigger.Payload;


        await Assert.That(payloadValue).IsOfType(typeof(EventStimulus));


        var payload = (EventStimulus)payloadValue!;
        await Assert.That(payload.EventName).IsEqualTo("Cancelled");

    }

    [Test]
    [DisplayName("A message start and a signal start with the same name correlate identically")]
    public async Task MessageAndSignalStartEvents_WithTheSameName_CorrelateIdentically()
    {
        // Correlation is on the resolved name alone -- the library's rule, not ours -- which is what makes a
        // .bpmn file portable: swapping a <messageEventDefinition> for a <signalEventDefinition> of the same name
        // must not change how an external publisher reaches this workflow.
        var messageProcess = RootScope("message-start", Message("Approved"));
        var signalProcess = RootScope("signal-start", Signal("Approved"));

        var messageTrigger = (await Assert.That(await IndexAsync(messageProcess)).HasSingleItem())!;

        var signalTrigger = (await Assert.That(await IndexAsync(signalProcess)).HasSingleItem())!;


        await Assert.That(signalTrigger.Name).IsEqualTo(messageTrigger.Name);

        await Assert.That(signalTrigger.Hash).IsEqualTo(messageTrigger.Hash);

    }

    [Test]
    [DisplayName("A recurring interval timer start registers through Elsa.Scheduling's own Timer path")]
    public async Task RecurringIntervalTimerStart_RegistersThroughTheSchedulingPath()
    {
        var process = RootScope("timer-start", TimerInterval("PT1H"));

        var trigger = (await Assert.That(await IndexAsync(process)).HasSingleItem())!;


        // Elsa.Scheduling's DefaultTriggerScheduler discovers a Timer trigger purely by this name -- reusing it,
        // rather than a BPMN-specific one, is what puts a recurring BPMN timer start through the same scheduling
        // path as Elsa.Scheduling's own Timer activity, unmodified.
        await Assert.That(trigger.Name).IsEqualTo(SchedulingStimulusNames.Timer);


        var payloadValue = trigger.Payload;


        await Assert.That(payloadValue).IsOfType(typeof(TimerTriggerPayload));


        var payload = (TimerTriggerPayload)payloadValue!;
        await Assert.That(payload.Interval).IsEqualTo(TimeSpan.FromHours(1));

    }

    [Test]
    [DisplayName("A recurring cron timer start registers through Elsa.Scheduling's own Cron path")]
    public async Task RecurringCronTimerStart_RegistersThroughTheSchedulingPath()
    {
        var process = RootScope("cron-start", TimerCron("0 0 * * *"));

        var trigger = (await Assert.That(await IndexAsync(process)).HasSingleItem())!;


        await Assert.That(trigger.Name).IsEqualTo(SchedulingStimulusNames.Cron);


        var payloadValue = trigger.Payload;


        await Assert.That(payloadValue).IsOfType(typeof(CronTriggerPayload));


        var payload = (CronTriggerPayload)payloadValue!;
        await Assert.That(payload.CronExpression).IsEqualTo("0 0 * * *");

    }

    [Test]
    [DisplayName("A message start and a recurring timer start on the same process both register, each independently matchable on its own name")]
    public async Task MessageStartAndRecurringTimerStart_BothRegisterIndependently()
    {
        // Before NamedTriggerPayload, TriggerIndexingContext.TriggerName was a single field the indexer read once
        // after every payload was collected: the last kind processed (the timer here) claimed the name for every
        // row, so the message row was hashed under a name no PublishEvent would ever compute. Each row must carry,
        // and be hashed from, its own name.
        var process = RootScope("mixed-start", Message("OrderPlaced"), TimerInterval("PT1H"));

        var triggers = await IndexAsync(process);
        var hasher = _services.GetRequiredService<IStimulusHasher>();

        var messageTrigger = (await Assert.That(triggers).HasSingleItem(trigger => trigger.Payload is EventStimulus))!;

        await Assert.That(messageTrigger.Name).IsEqualTo(RuntimeStimulusNames.Event);

        await Assert.That(messageTrigger.Hash).IsEqualTo(hasher.Hash(RuntimeStimulusNames.Event, messageTrigger.Payload));


        var timerTrigger = (await Assert.That(triggers).HasSingleItem(trigger => trigger.Payload is TimerTriggerPayload))!;

        await Assert.That(timerTrigger.Name).IsEqualTo(SchedulingStimulusNames.Timer);

        await Assert.That(timerTrigger.Hash).IsEqualTo(hasher.Hash(SchedulingStimulusNames.Timer, timerTrigger.Payload));

    }

    [Test]
    [DisplayName("Two start events resolving the same message name collapse to one trigger, without swallowing a genuinely distinct one")]
    public async Task DuplicateStartEvents_CollapseToOneTrigger_WithoutSwallowingDistinctOnes()
    {
        // Duplicate name twice, and a third, distinct name once: StimulusSender starts the workflow once per
        // matched StoredTrigger row, so two rows for the same resolved name would start the workflow twice for one
        // inbound stimulus -- but a distinct name must still register on its own. Read via GetRawTriggersAsync,
        // the undiffed list BpmnProcess itself produces, so a missing dedup guard shows up here even though the
        // store diff downstream would otherwise coincidentally collapse identical rows on a first index.
        var process = RootScope("dup-start", Message("OrderPlaced"), Message("OrderPlaced"), Message("OrderCancelled"));

        var triggers = await GetRawTriggersAsync(process);

        await Assert.That(triggers.Count).IsEqualTo(2);


        var eventNames = triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>().Select(stimulus => stimulus.EventName).OrderBy(name => name).ToList();
        await Assert.That(eventNames).IsEquivalentTo(new[] { "OrderCancelled", "OrderPlaced" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);

    }

    [Test]
    [DisplayName("A malformed timer interval is refused for its own start event; every other valid start event on the process still registers")]
    public async Task MalformedTimerInterval_RefusesOnlyItsOwnStartEvent()
    {
        // TriggerIndexer.TryGetTriggerDataAsync catches around the whole GetTriggerPayloadsAsync call, so an
        // exception left to propagate from here would discard every start on the process, not just the bad one.
        var process = RootScope("malformed-timer", TimerInterval("not-an-iso-8601-duration"), Message("OrderPlaced"));

        var triggers = await IndexAsync(process);

        await Assert.That(triggers).DoesNotContain(trigger => trigger.Payload is TimerTriggerPayload);


        var payload = (await Assert.That(triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>()).HasSingleItem())!;

        await Assert.That(payload.EventName).IsEqualTo("OrderPlaced");

    }

    [Test]
    [DisplayName("A non-positive timer interval is refused for its own start event; every other valid start event on the process still registers ($isoInterval)")]
    [Arguments("PT0S")]
    [Arguments("-PT1S")]
    public async Task NonPositiveTimerInterval_RefusesOnlyItsOwnStartEvent(string isoInterval)
    {
        // XmlConvert.ToTimeSpan parses both PT0S and a negative duration without complaint. Elsa.Scheduling's
        // scheduler substitutes a ~1ms delay whenever a trigger's next execution time is non-positive, so
        // registering either as a recurring timer trigger would turn a well-formed .bpmn document into a hot loop.
        var process = RootScope("non-positive-timer", TimerInterval(isoInterval), Message("OrderPlaced"));

        var triggers = await IndexAsync(process);

        await Assert.That(triggers).DoesNotContain(trigger => trigger.Payload is TimerTriggerPayload);


        var payload = (await Assert.That(triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>()).HasSingleItem())!;

        await Assert.That(payload.EventName).IsEqualTo("OrderPlaced");

    }

    [Test]
    [DisplayName("A sub-resolution timer interval is refused for its own start event; every other valid start event on the process still registers")]
    public async Task SubResolutionTimerInterval_RefusesOnlyItsOwnStartEvent()
    {
        // PT0.0000001S parses to a single tick -- positive, so it passes the non-positive check, but far below the
        // ~1ms floor Elsa.Scheduling's own ScheduledRecurringTask.SetupTimer substitutes whenever it computes a
        // non-positive delay. Left unrefused, this collapses to that same substituted delay every time it rearms,
        // which is the hot loop the non-positive guard was meant to close, one step down.
        var process = RootScope("sub-resolution-timer", TimerInterval("PT0.0000001S"), Message("OrderPlaced"));

        var triggers = await IndexAsync(process);

        await Assert.That(triggers).DoesNotContain(trigger => trigger.Payload is TimerTriggerPayload);


        var payload = (await Assert.That(triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>()).HasSingleItem())!;

        await Assert.That(payload.EventName).IsEqualTo("OrderPlaced");

    }

    [Test]
    [DisplayName("A timer interval exactly at the resolution floor registers")]
    public async Task TimerIntervalAtTheResolutionFloor_Registers()
    {
        // Pinned at exactly the floor -- not comfortably above it -- so this test would fail against any floor set
        // higher than the real one, and could not pass by coincidence against a floor set lower.
        var process = RootScope("floor-timer", TimerInterval("PT0.001S"));

        // Inspect the raw trigger here rather than publishing the indexed-trigger notification. Publishing a
        // deliberately 1ms recurring trigger arms a real timer, which can race this test's service-provider
        // teardown before the assertion has even finished.
        var trigger = (await Assert.That(await GetRawTriggersAsync(process)).HasSingleItem())!;


        var payloadValue = trigger.Payload;


        await Assert.That(payloadValue).IsOfType(typeof(TimerTriggerPayload));


        var payload = (TimerTriggerPayload)payloadValue!;
        await Assert.That(payload.Interval).IsEqualTo(TimeSpan.FromMilliseconds(1));

    }

    [Test]
    [DisplayName("A root scope whose start events are all plain registers no trigger, not even a placeholder")]
    public async Task PlainStartRootScope_RegistersNoTrigger()
    {
        // A plain start is how a process is started directly, through the workflow execution API, never by a
        // stimulus, so there is nothing to register. Read raw, before the store diff: this is the same list
        // ValidateWorkflowRequestHandler iterates, and a null-payload row in it is what refused publication (#8078).
        var process = RootScope("plain-start");

        await Assert.That(await GetRawTriggersAsync(process)).IsEmpty();

    }

    [Test]
    [DisplayName("A plain start beside a message start does not stop the message start from registering")]
    public async Task PlainStartBesideMessageStart_MessageStillRegisters()
    {
        // Only a scope with no event-defined start at all declines; one plain start among others must not.
        var process = new BpmnProcess
        {
            Id = "plain-and-message-start",
            IsRootScope = true,
            Process = new BpmnProcessBuilder("plain-and-message-start")
                .StartEvent("plain-start")
                .StartEvent("message-start", null, Message("OrderPlaced"))
                .EndEvent("end")
                .ConnectSequence("plain-start", "end")
                .ConnectSequence("message-start", "end")
                .Build()
        };

        var trigger = (await Assert.That(await GetRawTriggersAsync(process)).HasSingleItem())!;


        var payloadValue = trigger.Payload;


        await Assert.That(payloadValue).IsOfType(typeof(EventStimulus));


        var payload = (EventStimulus)payloadValue!;
        await Assert.That(payload.EventName).IsEqualTo("OrderPlaced");

    }

    [Test]
    [DisplayName("A root scope whose only event-defined start registers nothing keeps the placeholder row publish validation reports")]
    public async Task RootScopeWhoseOnlyEventDefinedStartIsRefused_KeepsThePlaceholderRow()
    {
        // The direction that could be mistaken for success: a start event declaring a timer this scope refuses to
        // register is not a plain start. Dropping the placeholder here would let the process publish looking fine
        // with a timer that never fires.
        var process = RootScope("refused-timer-only", TimerInterval("not-an-iso-8601-duration"));

        var trigger = (await Assert.That(await GetRawTriggersAsync(process)).HasSingleItem())!;


        await Assert.That(trigger.Payload).IsNull();

    }

    [Test]
    [DisplayName("A BPMN-nested plain-start scope keeps its placeholder row, whatever its own flag says")]
    public async Task BpmnNestedPlainStartScope_KeepsThePlaceholderRow()
    {
        // #8078 changes root position only. A nested scope is opted out by the graph check before the plain-start
        // check is ever reached, so it leaves the indexer's placeholder exactly as the other nested cases do.
        var nested = Scope("inner");
        nested.IsRootScope = true;

        var outer = RootScope("outer", Message("OuterOnly"));
        outer.Activities.Add(nested);

        await AssertNestedScopeContributedOnlyTheUnavoidablePlaceholderRow(await IndexAsync(outer), nested.Id);
    }

    [Test]
    [DisplayName("A BPMN-nested scope registers no triggers, whatever its own flag says")]
    public async Task BpmnNestedScope_RegistersNoTriggers()
    {
        // The flag is set wrong on purpose: only the graph -- not the flag -- is allowed to decide this.
        var nested = Scope("inner", Message("Ignored"));
        nested.IsRootScope = true;

        var outer = RootScope("outer", Message("OuterOnly"));
        outer.Activities.Add(nested);

        var triggers = await IndexAsync(outer);

        // The outer scope's own start event is the only name that ever reaches the trigger store.
        var payloads = triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>().ToList();
        var payload = (await Assert.That(payloads).HasSingleItem())!;

        await Assert.That(payload.EventName).IsEqualTo("OuterOnly");


        await AssertNestedScopeContributedOnlyTheUnavoidablePlaceholderRow(triggers, nested.Id);
    }

    [Test]
    [DisplayName("A scope nested through an intermediate Flowchart registers no triggers")]
    public async Task ScopeNestedThroughAnIntermediateFlowchart_RegistersNoTriggers()
    {
        // This is the gap #7926's applier-level refusal cannot see: it only ever inspects work bound directly to
        // the enclosing BpmnProcess, so a scope reached through an intervening Flowchart sails past it. Trigger
        // indexing has to catch it instead.
        var nested = Scope("inner", Message("Ignored"));
        nested.IsRootScope = true;

        var flowchart = new Flowchart
        {
            Start = nested,
            Activities = { nested }
        };

        var outer = RootScope("outer", Message("OuterOnly"));
        outer.Activities.Add(flowchart);

        var triggers = await IndexAsync(outer);

        var payloads = triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>().ToList();
        var payload = (await Assert.That(payloads).HasSingleItem())!;

        await Assert.That(payload.EventName).IsEqualTo("OuterOnly");


        await AssertNestedScopeContributedOnlyTheUnavoidablePlaceholderRow(triggers, nested.Id);
    }

    private async Task<IReadOnlyCollection<StoredTrigger>> IndexAsync(IActivity root)
    {
        var workflow = await BuildWorkflowAsync(root);
        var indexer = _services.GetRequiredService<ITriggerIndexer>();
        var result = await indexer.IndexTriggersAsync(workflow);
        var addedTriggers = result.AddedTriggers.ToList();

        // IndexTriggersAsync publishes WorkflowTriggersIndexed, so scheduling triggers are armed before it
        // returns. These tests only inspect the indexed records; release any schedules they created while the
        // provider is still alive instead of leaving timers behind for test teardown.
        var scheduler = _services.GetRequiredService<IScheduler>();
        foreach (var trigger in addedTriggers)
            await scheduler.ClearScheduleAsync(trigger.Id);

        return addedTriggers;
    }

    /// <summary>
    /// Indexes without the store diff <see cref="IndexAsync"/> goes through: <c>Diff.For</c> compares by
    /// <c>Name</c>+<c>Hash</c>+<c>Payload</c>+<c>ActivityId</c>, so two identical rows in the raw list it diffs would
    /// already collapse to one <em>added</em> row on a first index, which would mask a missing dedup guard in
    /// <see cref="BpmnProcess"/> itself. This calls <see cref="ITriggerIndexer.GetTriggersAsync"/> instead -- the
    /// same raw, undiffed list <c>ValidateWorkflowRequestHandler</c> iterates -- so a duplicate BpmnProcess emits is
    /// visible here even when the diff downstream would have hidden it.
    /// </summary>
    private async Task<IReadOnlyCollection<StoredTrigger>> GetRawTriggersAsync(IActivity root)
    {
        var workflow = await BuildWorkflowAsync(root);
        var indexer = _services.GetRequiredService<ITriggerIndexer>();

        return (await indexer.GetTriggersAsync(workflow)).ToList();
    }

    private async Task<Workflow> BuildWorkflowAsync(IActivity root)
    {
        await _services.PopulateRegistriesAsync();

        return new Workflow
        {
            Identity = new(Guid.NewGuid().ToString(), 1, Guid.NewGuid().ToString()),
            Publication = new(IsLatest: true, IsPublished: true),
            Root = root
        };
    }

    /// <summary>
    /// A nested scope contributes no message, signal, or timer payload of its own -- the thing this guard actually
    /// protects. It still owns exactly one row: <c>Elsa.Workflows.Runtime.TriggerIndexer</c> adds a
    /// <c>Payload = null</c> placeholder for any <c>ITrigger</c> whose <c>GetTriggerPayloadsAsync</c> returns no
    /// payload at all, unless the trigger declares through <c>TriggerIndexingContext.RegistersNoTriggers</c> that it
    /// deliberately registers none. <see cref="BpmnProcess"/> declares that only for a root scope with no
    /// event-defined start (#8078); a nested scope's opt-out is decided by the graph before that point and is left as
    /// it was. That placeholder is inert -- nothing external can ever address a <c>null</c> payload.
    /// </summary>
    private static async Task AssertNestedScopeContributedOnlyTheUnavoidablePlaceholderRow(IReadOnlyCollection<StoredTrigger> triggers, string nestedScopeActivityId)
    {
        var nestedScopeTrigger = (await Assert.That(triggers).HasSingleItem(trigger => trigger.ActivityId == nestedScopeActivityId))!;

        await Assert.That(nestedScopeTrigger.Payload).IsNull();

    }

    /// <summary>A scope marked as the workflow's own entry point.</summary>
    private static BpmnProcess RootScope(string id, params BpmnEventDefinition[] startEventDefinitions)
    {
        var scope = Scope(id, startEventDefinitions);
        scope.IsRootScope = true;
        return scope;
    }

    /// <summary>A scope with no opinion of its own about root position.</summary>
    private static BpmnProcess Scope(string id, params BpmnEventDefinition[] startEventDefinitions)
    {
        var definition = new BpmnProcessBuilder(id)
            .StartEvent("start", null, startEventDefinitions)
            .EndEvent("end")
            .ConnectSequence("start", "end")
            .Build();

        return new BpmnProcess
        {
            Id = id,
            Process = definition
        };
    }

    private static BpmnEventDefinition Message(string name) =>
        new(BpmnEventDefinitionTypes.Message, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Name] = name });

    private static BpmnEventDefinition Signal(string name) =>
        new(BpmnEventDefinitionTypes.Signal, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Name] = name });

    private static BpmnEventDefinition TimerInterval(string isoDuration) =>
        new(BpmnEventDefinitionTypes.Timer, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Interval] = isoDuration });

    private static BpmnEventDefinition TimerCron(string cron) =>
        new(BpmnEventDefinitionTypes.Timer, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Cron] = cron });
}

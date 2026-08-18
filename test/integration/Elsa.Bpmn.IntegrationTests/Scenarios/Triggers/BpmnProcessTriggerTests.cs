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
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.Triggers;

/// <summary>
/// A <see cref="BpmnProcess"/>'s own event-defined start events register as workflow triggers when the scope is the
/// workflow's root -- and register nothing when the scope is nested, directly or through an intermediate
/// <c>Flowchart</c>, no matter what <see cref="BpmnProcess.IsRootScope"/> itself says.
/// </summary>
public class BpmnProcessTriggerTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _services = new TestApplicationBuilder(testOutputHelper)
        .ConfigureElsa(elsa => elsa.UseBpmn())
        .Build();

    [Fact(DisplayName = "A message start event registers exactly one trigger, on the resolved name")]
    public async Task MessageStartEvent_RegistersOneTriggerOnName()
    {
        var process = RootScope("message-start", Message("OrderPlaced"));

        var trigger = Assert.Single(await IndexAsync(process));

        var payload = Assert.IsType<EventStimulus>(trigger.Payload);
        Assert.Equal("OrderPlaced", payload.EventName);
    }

    [Fact(DisplayName = "A signal start event registers exactly one trigger, on the resolved name")]
    public async Task SignalStartEvent_RegistersOneTriggerOnName()
    {
        var process = RootScope("signal-start", Signal("Cancelled"));

        var trigger = Assert.Single(await IndexAsync(process));

        var payload = Assert.IsType<EventStimulus>(trigger.Payload);
        Assert.Equal("Cancelled", payload.EventName);
    }

    [Fact(DisplayName = "A message start and a signal start with the same name correlate identically")]
    public async Task MessageAndSignalStartEvents_WithTheSameName_CorrelateIdentically()
    {
        // Correlation is on the resolved name alone -- the library's rule, not ours -- which is what makes a
        // .bpmn file portable: swapping a <messageEventDefinition> for a <signalEventDefinition> of the same name
        // must not change how an external publisher reaches this workflow.
        var messageProcess = RootScope("message-start", Message("Approved"));
        var signalProcess = RootScope("signal-start", Signal("Approved"));

        var messageTrigger = Assert.Single(await IndexAsync(messageProcess));
        var signalTrigger = Assert.Single(await IndexAsync(signalProcess));

        Assert.Equal(messageTrigger.Name, signalTrigger.Name);
        Assert.Equal(messageTrigger.Hash, signalTrigger.Hash);
    }

    [Fact(DisplayName = "A recurring interval timer start registers through Elsa.Scheduling's own Timer path")]
    public async Task RecurringIntervalTimerStart_RegistersThroughTheSchedulingPath()
    {
        var process = RootScope("timer-start", TimerInterval("PT1H"));

        var trigger = Assert.Single(await IndexAsync(process));

        // Elsa.Scheduling's DefaultTriggerScheduler discovers a Timer trigger purely by this name -- reusing it,
        // rather than a BPMN-specific one, is what puts a recurring BPMN timer start through the same scheduling
        // path as Elsa.Scheduling's own Timer activity, unmodified.
        Assert.Equal(SchedulingStimulusNames.Timer, trigger.Name);

        var payload = Assert.IsType<TimerTriggerPayload>(trigger.Payload);
        Assert.Equal(TimeSpan.FromHours(1), payload.Interval);
    }

    [Fact(DisplayName = "A recurring cron timer start registers through Elsa.Scheduling's own Cron path")]
    public async Task RecurringCronTimerStart_RegistersThroughTheSchedulingPath()
    {
        var process = RootScope("cron-start", TimerCron("0 0 * * *"));

        var trigger = Assert.Single(await IndexAsync(process));

        Assert.Equal(SchedulingStimulusNames.Cron, trigger.Name);

        var payload = Assert.IsType<CronTriggerPayload>(trigger.Payload);
        Assert.Equal("0 0 * * *", payload.CronExpression);
    }

    [Fact(DisplayName = "A message start and a recurring timer start on the same process both register, each independently matchable on its own name")]
    public async Task MessageStartAndRecurringTimerStart_BothRegisterIndependently()
    {
        // Before NamedTriggerPayload, TriggerIndexingContext.TriggerName was a single field the indexer read once
        // after every payload was collected: the last kind processed (the timer here) claimed the name for every
        // row, so the message row was hashed under a name no PublishEvent would ever compute. Each row must carry,
        // and be hashed from, its own name.
        var process = RootScope("mixed-start", Message("OrderPlaced"), TimerInterval("PT1H"));

        var triggers = await IndexAsync(process);
        var hasher = _services.GetRequiredService<IStimulusHasher>();

        var messageTrigger = Assert.Single(triggers, trigger => trigger.Payload is EventStimulus);
        Assert.Equal(RuntimeStimulusNames.Event, messageTrigger.Name);
        Assert.Equal(hasher.Hash(RuntimeStimulusNames.Event, messageTrigger.Payload), messageTrigger.Hash);

        var timerTrigger = Assert.Single(triggers, trigger => trigger.Payload is TimerTriggerPayload);
        Assert.Equal(SchedulingStimulusNames.Timer, timerTrigger.Name);
        Assert.Equal(hasher.Hash(SchedulingStimulusNames.Timer, timerTrigger.Payload), timerTrigger.Hash);
    }

    [Fact(DisplayName = "Two start events resolving the same message name collapse to one trigger, without swallowing a genuinely distinct one")]
    public async Task DuplicateStartEvents_CollapseToOneTrigger_WithoutSwallowingDistinctOnes()
    {
        // Duplicate name twice, and a third, distinct name once: StimulusSender starts the workflow once per
        // matched StoredTrigger row, so two rows for the same resolved name would start the workflow twice for one
        // inbound stimulus -- but a distinct name must still register on its own. Read via GetRawTriggersAsync,
        // the undiffed list BpmnProcess itself produces, so a missing dedup guard shows up here even though the
        // store diff downstream would otherwise coincidentally collapse identical rows on a first index.
        var process = RootScope("dup-start", Message("OrderPlaced"), Message("OrderPlaced"), Message("OrderCancelled"));

        var triggers = await GetRawTriggersAsync(process);

        Assert.Equal(2, triggers.Count);

        var eventNames = triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>().Select(stimulus => stimulus.EventName).OrderBy(name => name).ToList();
        Assert.Equal(new[] { "OrderCancelled", "OrderPlaced" }, eventNames);
    }

    [Fact(DisplayName = "A malformed timer interval is refused for its own start event; every other valid start event on the process still registers")]
    public async Task MalformedTimerInterval_RefusesOnlyItsOwnStartEvent()
    {
        // TriggerIndexer.TryGetTriggerDataAsync catches around the whole GetTriggerPayloadsAsync call, so an
        // exception left to propagate from here would discard every start on the process, not just the bad one.
        var process = RootScope("malformed-timer", TimerInterval("not-an-iso-8601-duration"), Message("OrderPlaced"));

        var triggers = await IndexAsync(process);

        Assert.DoesNotContain(triggers, trigger => trigger.Payload is TimerTriggerPayload);

        var payload = Assert.Single(triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>());
        Assert.Equal("OrderPlaced", payload.EventName);
    }

    [Theory(DisplayName = "A non-positive timer interval is refused for its own start event; every other valid start event on the process still registers")]
    [InlineData("PT0S")]
    [InlineData("-PT1S")]
    public async Task NonPositiveTimerInterval_RefusesOnlyItsOwnStartEvent(string isoInterval)
    {
        // XmlConvert.ToTimeSpan parses both PT0S and a negative duration without complaint. Elsa.Scheduling's
        // scheduler substitutes a ~1ms delay whenever a trigger's next execution time is non-positive, so
        // registering either as a recurring timer trigger would turn a well-formed .bpmn document into a hot loop.
        var process = RootScope("non-positive-timer", TimerInterval(isoInterval), Message("OrderPlaced"));

        var triggers = await IndexAsync(process);

        Assert.DoesNotContain(triggers, trigger => trigger.Payload is TimerTriggerPayload);

        var payload = Assert.Single(triggers.Select(trigger => trigger.Payload).OfType<EventStimulus>());
        Assert.Equal("OrderPlaced", payload.EventName);
    }

    [Fact(DisplayName = "A BPMN-nested scope registers no triggers, whatever its own flag says")]
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
        var payload = Assert.Single(payloads);
        Assert.Equal("OuterOnly", payload.EventName);

        AssertNestedScopeContributedOnlyTheUnavoidablePlaceholderRow(triggers, nested.Id);
    }

    [Fact(DisplayName = "A scope nested through an intermediate Flowchart registers no triggers")]
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
        var payload = Assert.Single(payloads);
        Assert.Equal("OuterOnly", payload.EventName);

        AssertNestedScopeContributedOnlyTheUnavoidablePlaceholderRow(triggers, nested.Id);
    }

    private async Task<IReadOnlyCollection<StoredTrigger>> IndexAsync(IActivity root)
    {
        var workflow = await BuildWorkflowAsync(root);
        var indexer = _services.GetRequiredService<ITriggerIndexer>();
        var result = await indexer.IndexTriggersAsync(workflow);

        return result.AddedTriggers.ToList();
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
    /// <c>Payload = null</c> placeholder for <i>any</i> <c>ITrigger</c> whose <c>GetTriggerPayloadsAsync</c> returns
    /// no payload at all, whether that activity is a legitimately root BPMN process with only a plain start event or
    /// a nested one this guard correctly emptied out. That placeholder is inert -- nothing external can ever address
    /// a <c>null</c> payload -- and clearing it would mean changing that indexer's own fallback for every trigger
    /// kind in the runtime, not just BPMN's, which is well outside this guard's reach.
    /// </summary>
    private static void AssertNestedScopeContributedOnlyTheUnavoidablePlaceholderRow(IReadOnlyCollection<StoredTrigger> triggers, string nestedScopeActivityId)
    {
        var nestedScopeTrigger = Assert.Single(triggers, trigger => trigger.ActivityId == nestedScopeActivityId);
        Assert.Null(nestedScopeTrigger.Payload);
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

using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Bookmarks;
using Elsa.Scheduling;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Stimuli;
using Microsoft.Extensions.DependencyInjection;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing;

/// <summary>
/// Characterises how the trigger kinds that ship with Elsa are indexed, so that a change to the indexer that alters any of their rows is visible.
/// The invariant asserted for every row: its hash is the one a publisher computes from that row's own name and payload.
/// </summary>
public class ExistingTriggerKindsTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IStimulusHasher _stimulusHasher;

    public ExistingTriggerKindsTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseHttp().AddActivity<MultiStimulusTrigger>())
            .Build();

        _triggerIndexer = _services.GetRequiredService<ITriggerIndexer>();
        _stimulusHasher = _services.GetRequiredService<IStimulusHasher>();
    }

    [Test]
    [DisplayName("Each built-in trigger kind indexes one row per payload under its own stimulus name")]
    public async Task GetTriggersAsync_BuiltInTriggerKinds_IndexUnderTheirOwnStimulusName()
    {
        var startAt = new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var interval = TimeSpan.FromHours(3);
        var triggers = await IndexAsync(
            new Event("order-received")
            {
                Id = "event"
            },
            new Timer(interval)
            {
                Id = "timer"
            },
            new Cron
            {
                Id = "cron",
                CronExpression = new("0 0 * * *")
            },
            new StartAt(startAt)
            {
                Id = "start-at"
            },
            new HttpEndpoint
            {
                Id = "http-endpoint",
                Path = new("/orders"),
                SupportedMethods = new(new[]
                {
                    "GET",
                    "POST"
                })
            });

        // Event: names its payload through the shared trigger name.
        var eventTrigger = await Assert.That(triggers["event"]).HasSingleItem();
        await AssertMatchableAsync(eventTrigger, RuntimeStimulusNames.Event);
        await Assert.That(eventTrigger.Payload).IsEqualTo(new EventStimulus("order-received"));

        // Timer: names its payload through the shared trigger name. Its start time is relative to the clock, so only the interval is pinned.
        var timerTrigger = await Assert.That(triggers["timer"]).HasSingleItem();
        await AssertMatchableAsync(timerTrigger, SchedulingStimulusNames.Timer);
        await Assert.That(timerTrigger.Payload).IsOfType(typeof(TimerTriggerPayload));
        var timerPayload = (TimerTriggerPayload)timerTrigger.Payload!;
        await Assert.That(timerPayload.Interval).IsEqualTo(interval);

        // Cron and StartAt: never assign a trigger name, so they fall back to the activity type name.
        var cronTrigger = await Assert.That(triggers["cron"]).HasSingleItem();
        await AssertMatchableAsync(cronTrigger, SchedulingStimulusNames.Cron);
        await Assert.That(cronTrigger.Payload).IsEqualTo(new CronTriggerPayload("0 0 * * *"));

        var startAtTrigger = await Assert.That(triggers["start-at"]).HasSingleItem();
        await AssertMatchableAsync(startAtTrigger, SchedulingStimulusNames.StartAt);
        await Assert.That(startAtTrigger.Payload).IsEqualTo(new StartAtPayload(startAt));

        // HttpEndpoint: one row per supported method, all sharing a single trigger name.
        var httpTriggers = triggers["http-endpoint"];
        await Assert.That(httpTriggers.Count).IsEqualTo(2);
        foreach (var trigger in httpTriggers)
            await AssertMatchableAsync(trigger, HttpStimulusNames.HttpEndpoint);

        var httpPayloads = new List<(string Path, string Method)>();
        foreach (var trigger in httpTriggers)
        {
            await Assert.That(trigger.Payload).IsOfType(typeof(HttpEndpointBookmarkPayload));
            var payload = (HttpEndpointBookmarkPayload)trigger.Payload!;
            httpPayloads.Add((payload.Path, payload.Method));
        }

        await Assert.That(httpPayloads.OrderBy(x => x.Method)).IsEquivalentTo(
            [
                ("/orders", "get"),
                ("/orders", "post")
            ],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("A trigger that contributes named payloads indexes each of them under its own stimulus name")]
    public async Task GetTriggersAsync_NamedPayloads_IndexUnderTheirOwnStimulusName()
    {
        var triggers = await IndexAsync(new MultiStimulusTrigger
        {
            Id = "multi"
        });

        // Both rows must be matchable: the event by a published event, the timer by the trigger scheduler.
        var rows = triggers["multi"];
        await Assert.That(rows.Count).IsEqualTo(2);

        var eventRow = rows.Single(x => x.Name == RuntimeStimulusNames.Event);
        await AssertMatchableAsync(eventRow, RuntimeStimulusNames.Event);
        await Assert.That(eventRow.Payload).IsEqualTo(new EventStimulus(MultiStimulusTrigger.EventName));
        await Assert.That(eventRow.Hash).IsEqualTo(_stimulusHasher.Hash(RuntimeStimulusNames.Event, new EventStimulus(MultiStimulusTrigger.EventName)));

        var timerRow = rows.Single(x => x.Name == SchedulingStimulusNames.Timer);
        await AssertMatchableAsync(timerRow, SchedulingStimulusNames.Timer);
        await Assert.That(timerRow.Payload).IsOfType(typeof(TimerTriggerPayload));
        var timerPayload = (TimerTriggerPayload)timerRow.Payload!;
        await Assert.That(timerPayload.Interval).IsEqualTo(MultiStimulusTrigger.Interval);

        // The name is also what DefaultTriggerScheduler filters on, so the timer row is schedulable without any trigger-specific machinery.
        await Assert.That(rows.Filter<Timer>()).HasSingleItem();
    }

    private async Task<IDictionary<string, IList<StoredTrigger>>> IndexAsync(params IActivity[] activities)
    {
        await _services.PopulateRegistriesAsync();

        foreach (var activity in activities)
            activity.SetCanStartWorkflow(true);

        var workflow = new Workflow(new Sequence
        {
            Activities = activities
        })
        {
            Identity = new("triggers", 1, "triggers:1")
        };

        // Assign identities the way workflow materialization does, so that activity inputs resolve during indexing.
        var workflowGraph = await _services.GetRequiredService<IWorkflowGraphBuilder>().BuildAsync(workflow);
        var triggers = await _triggerIndexer.GetTriggersAsync(workflowGraph.Workflow, default);
        return triggers.GroupBy(x => x.ActivityId).ToDictionary(x => x.Key, IList<StoredTrigger> (x) => x.ToList());
    }

    private async Task AssertMatchableAsync(StoredTrigger trigger, string expectedName)
    {
        await Assert.That(trigger.Name).IsEqualTo(expectedName);
        await Assert.That(trigger.Hash).IsEqualTo(_stimulusHasher.Hash(expectedName, trigger.Payload));
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}

/// <summary>
/// A trigger that registers an event stimulus and a timer stimulus, reusing the existing stimulus extension methods.
/// </summary>
public class MultiStimulusTrigger : Trigger
{
    public const string EventName = "bpmn-message-start";
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context)
    {
        // Each extension method assigns the shared trigger name as a side effect; the payload's own name is what counts.
        var eventStimulus = context.GetEventStimulus(EventName);
        yield return new NamedTriggerPayload(RuntimeStimulusNames.Event, eventStimulus);

        var timerStimulus = context.GetTimerTriggerStimulus(Interval);
        yield return new NamedTriggerPayload(SchedulingStimulusNames.Timer, timerStimulus);
    }
}

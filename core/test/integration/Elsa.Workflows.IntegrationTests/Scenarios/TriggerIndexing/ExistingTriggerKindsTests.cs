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
using Xunit.Abstractions;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing;

/// <summary>
/// Characterises how the trigger kinds that ship with Elsa are indexed, so that a change to the indexer that alters any of their rows is visible.
/// The invariant asserted for every row: its hash is the one a publisher computes from that row's own name and payload.
/// </summary>
public class ExistingTriggerKindsTests
{
    private readonly IServiceProvider _services;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IStimulusHasher _stimulusHasher;

    public ExistingTriggerKindsTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseHttp().AddActivity<MultiStimulusTrigger>())
            .Build();

        _triggerIndexer = _services.GetRequiredService<ITriggerIndexer>();
        _stimulusHasher = _services.GetRequiredService<IStimulusHasher>();
    }

    [Fact(DisplayName = "Each built-in trigger kind indexes one row per payload under its own stimulus name")]
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
        var eventTrigger = Assert.Single(triggers["event"]);
        AssertMatchable(eventTrigger, RuntimeStimulusNames.Event);
        Assert.Equal(new EventStimulus("order-received"), eventTrigger.Payload);

        // Timer: names its payload through the shared trigger name. Its start time is relative to the clock, so only the interval is pinned.
        var timerTrigger = Assert.Single(triggers["timer"]);
        AssertMatchable(timerTrigger, SchedulingStimulusNames.Timer);
        Assert.Equal(interval, Assert.IsType<TimerTriggerPayload>(timerTrigger.Payload).Interval);

        // Cron and StartAt: never assign a trigger name, so they fall back to the activity type name.
        var cronTrigger = Assert.Single(triggers["cron"]);
        AssertMatchable(cronTrigger, SchedulingStimulusNames.Cron);
        Assert.Equal(new CronTriggerPayload("0 0 * * *"), cronTrigger.Payload);

        var startAtTrigger = Assert.Single(triggers["start-at"]);
        AssertMatchable(startAtTrigger, SchedulingStimulusNames.StartAt);
        Assert.Equal(new StartAtPayload(startAt), startAtTrigger.Payload);

        // HttpEndpoint: one row per supported method, all sharing a single trigger name.
        var httpTriggers = triggers["http-endpoint"];
        Assert.Equal(2, httpTriggers.Count);
        Assert.All(httpTriggers, trigger => AssertMatchable(trigger, HttpStimulusNames.HttpEndpoint));
        Assert.Equal(
            [
                ("/orders", "get"),
                ("/orders", "post")
            ],
            httpTriggers.Select(x => Assert.IsType<HttpEndpointBookmarkPayload>(x.Payload)).Select(x => (x.Path, x.Method)).OrderBy(x => x.Method));
    }

    [Fact(DisplayName = "A trigger that contributes named payloads indexes each of them under its own stimulus name")]
    public async Task GetTriggersAsync_NamedPayloads_IndexUnderTheirOwnStimulusName()
    {
        var triggers = await IndexAsync(new MultiStimulusTrigger
        {
            Id = "multi"
        });

        // Both rows must be matchable: the event by a published event, the timer by the trigger scheduler.
        var rows = triggers["multi"];
        Assert.Equal(2, rows.Count);

        var eventRow = rows.Single(x => x.Name == RuntimeStimulusNames.Event);
        AssertMatchable(eventRow, RuntimeStimulusNames.Event);
        Assert.Equal(new EventStimulus(MultiStimulusTrigger.EventName), eventRow.Payload);
        Assert.Equal(_stimulusHasher.Hash(RuntimeStimulusNames.Event, new EventStimulus(MultiStimulusTrigger.EventName)), eventRow.Hash);

        var timerRow = rows.Single(x => x.Name == SchedulingStimulusNames.Timer);
        AssertMatchable(timerRow, SchedulingStimulusNames.Timer);
        Assert.Equal(MultiStimulusTrigger.Interval, Assert.IsType<TimerTriggerPayload>(timerRow.Payload).Interval);

        // The name is also what DefaultTriggerScheduler filters on, so the timer row is schedulable without any trigger-specific machinery.
        Assert.Single(rows.Filter<Timer>());
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

    private void AssertMatchable(StoredTrigger trigger, string expectedName)
    {
        Assert.Equal(expectedName, trigger.Name);
        Assert.Equal(_stimulusHasher.Hash(expectedName, trigger.Payload), trigger.Hash);
    }
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

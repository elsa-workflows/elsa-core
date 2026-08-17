using Elsa.Common.DistributedHosting;
using Elsa.Common.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Medallion.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

/// <summary>
/// Characterises how <see cref="TriggerIndexer"/> turns the payloads of an <see cref="ITrigger"/> into stored triggers.
/// The invariant under test: every stored row's <see cref="StoredTrigger.Hash"/> equals the hash a publisher computes from the row's own
/// <see cref="StoredTrigger.Name"/> and <see cref="StoredTrigger.Payload"/>, where that name is the one the trigger associated with that payload.
/// </summary>
public class TriggerIndexerTests
{
    private const string EventStimulusName = "Test.Event";
    private const string TimerStimulusName = "Test.Timer";

    private readonly IStimulusHasher _hasher;
    private readonly TriggerIndexer _indexer;

    public TriggerIndexerTests()
    {
        var typeRegistry = SerializationTypeRegistry.CreateDefault();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var activityRegistry = Substitute.For<IActivityRegistry>();
        var identityGenerator = Substitute.For<IIdentityGenerator>();

        activityRegistry.Find(Arg.Any<string>(), Arg.Any<int>()).Returns(new ActivityDescriptor());
        identityGenerator.GenerateId().Returns(_ => Guid.NewGuid().ToString("N"));
        _hasher = new StimulusHasher(new Hasher(typeRegistry));

        _indexer = new(
            new ActivityVisitor([], serviceProvider),
            Substitute.For<IWorkflowDefinitionService>(),
            Substitute.For<IExpressionEvaluator>(),
            identityGenerator,
            Substitute.For<ITriggerStore>(),
            activityRegistry,
            Substitute.For<INotificationSender>(),
            serviceProvider,
            _hasher,
            Substitute.For<IDistributedLockProvider>(),
            typeRegistry,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            NullLogger<TriggerIndexer>.Instance);
    }

    [Fact(DisplayName = "A payload without a name of its own is indexed under the activity type name")]
    public async Task GetTriggersAsync_PayloadWithoutName_UsesActivityTypeName()
    {
        var payload = new TestStimulus("a");
        var triggers = await IndexAsync(_ => [payload]);

        var trigger = Assert.Single(triggers);
        Assert.Equal(ActivityTypeNameHelper.GenerateTypeName<TestTrigger>(), trigger.Name);
        AssertMatchable(trigger, ActivityTypeNameHelper.GenerateTypeName<TestTrigger>(), payload);
    }

    [Fact(DisplayName = "An assigned trigger name applies to every payload the trigger returns")]
    public async Task GetTriggersAsync_AssignedTriggerName_AppliesToEveryPayload()
    {
        var first = new TestStimulus("a");
        var second = new TestStimulus("b");
        var triggers = await IndexAsync(context =>
        {
            context.TriggerName = EventStimulusName;
            return [first, second];
        });

        Assert.Collection(triggers,
            trigger => AssertMatchable(trigger, EventStimulusName, first),
            trigger => AssertMatchable(trigger, EventStimulusName, second));
    }

    [Fact(DisplayName = "The last assignment of the trigger name wins for payloads that do not carry a name")]
    public async Task GetTriggersAsync_TriggerNameAssignedTwice_LastWriteWinsForUnnamedPayloads()
    {
        var first = new TestStimulus("a");
        var second = new TestStimulus("b");
        var triggers = await IndexAsync(context =>
        {
            context.TriggerName = EventStimulusName;
            context.TriggerName = TimerStimulusName;
            return [first, second];
        });

        // Pinned deliberately: the shared field keeps its existing semantics. Payloads that need their own name must say so.
        Assert.Collection(triggers,
            trigger => AssertMatchable(trigger, TimerStimulusName, first),
            trigger => AssertMatchable(trigger, TimerStimulusName, second));
    }

    [Fact(DisplayName = "Each named payload is indexed under its own name, and each is matchable")]
    public async Task GetTriggersAsync_NamedPayloads_AreEachIndexedUnderTheirOwnName()
    {
        var eventStimulus = new TestStimulus("order-received");
        var timerStimulus = new TestStimulus("every-hour");
        var triggers = await IndexAsync(context =>
        {
            // Mirrors the stimulus extension methods, which each assign the shared name as a side effect.
            context.TriggerName = EventStimulusName;
            context.TriggerName = TimerStimulusName;
            return
            [
                new NamedTriggerPayload(EventStimulusName, eventStimulus),
                new NamedTriggerPayload(TimerStimulusName, timerStimulus)
            ];
        });

        Assert.Collection(triggers,
            trigger => AssertMatchable(trigger, EventStimulusName, eventStimulus),
            trigger => AssertMatchable(trigger, TimerStimulusName, timerStimulus));
    }

    [Fact(DisplayName = "Named and unnamed payloads can be mixed on a single trigger")]
    public async Task GetTriggersAsync_MixedPayloads_NameEachPayloadIndependently()
    {
        var named = new TestStimulus("named");
        var unnamed = new TestStimulus("unnamed");
        var triggers = await IndexAsync(context =>
        {
            context.TriggerName = TimerStimulusName;
            return [new NamedTriggerPayload(EventStimulusName, named), unnamed];
        });

        Assert.Collection(triggers,
            trigger => AssertMatchable(trigger, EventStimulusName, named),
            trigger => AssertMatchable(trigger, TimerStimulusName, unnamed));
    }

    [Fact(DisplayName = "A named payload is stored unwrapped")]
    public async Task GetTriggersAsync_NamedPayload_StoresTheInnerPayload()
    {
        var payload = new TestStimulus("a");
        var triggers = await IndexAsync(_ => [new NamedTriggerPayload(EventStimulusName, payload)]);

        // Storing the wrapper would produce a self-consistent but unmatchable row, and would hand the wrapper to payload validators.
        var trigger = Assert.Single(triggers);
        Assert.Same(payload, trigger.Payload);
        Assert.IsType<TestStimulus>(trigger.Payload);
    }

    [Fact(DisplayName = "A trigger returning no payloads still produces a single placeholder row")]
    public async Task GetTriggersAsync_NoPayloads_ProducesPlaceholderRow()
    {
        var triggers = await IndexAsync(_ => []);

        var trigger = Assert.Single(triggers);
        Assert.Null(trigger.Payload);
        Assert.Equal(ActivityTypeNameHelper.GenerateTypeName<TestTrigger>(), trigger.Name);
        Assert.Equal(_hasher.Hash(trigger.Name!, null), trigger.Hash);
    }

    [Fact(DisplayName = "A trigger that throws produces a single placeholder row")]
    public async Task GetTriggersAsync_ThrowingTrigger_ProducesPlaceholderRow()
    {
        var triggers = await IndexAsync(_ => throw new InvalidOperationException("Cannot resolve payloads."));

        var trigger = Assert.Single(triggers);
        Assert.Null(trigger.Payload);
        Assert.Equal(ActivityTypeNameHelper.GenerateTypeName<TestTrigger>(), trigger.Name);
    }

    [Theory(DisplayName = "A named payload requires a stimulus name")]
    [InlineData("")]
    [InlineData(" ")]
    public void NamedTriggerPayload_BlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => new NamedTriggerPayload(name, new TestStimulus("a")));
    }

    private async Task<ICollection<StoredTrigger>> IndexAsync(Func<TriggerIndexingContext, IEnumerable<object>> payloadsFactory)
    {
        var trigger = new TestTrigger
        {
            PayloadsFactory = payloadsFactory
        };
        trigger.SetCanStartWorkflow(true);
        var workflow = new Workflow(trigger)
        {
            Identity = new("definition-1", 1, "definition-1:1")
        };
        return (await _indexer.GetTriggersAsync(workflow, default)).ToList();
    }

    /// <summary>
    /// Asserts that the row carries the expected name and payload, and that its hash is the one a publisher of that same stimulus name computes.
    /// Also asserts that the hash is not the one that would result from labelling the payload with the other stimulus name in play.
    /// </summary>
    private void AssertMatchable(StoredTrigger trigger, string expectedName, object expectedPayload)
    {
        var otherName = expectedName == EventStimulusName ? TimerStimulusName : EventStimulusName;

        Assert.Equal(expectedName, trigger.Name);
        Assert.Same(expectedPayload, trigger.Payload);
        Assert.Equal(_hasher.Hash(expectedName, expectedPayload), trigger.Hash);
        Assert.NotEqual(_hasher.Hash(otherName, expectedPayload), trigger.Hash);
    }
}

public record TestStimulus(string Value);

public class TestTrigger : Trigger
{
    public Func<TriggerIndexingContext, IEnumerable<object>> PayloadsFactory { get; set; } = _ => [];

    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => PayloadsFactory(context);
}

using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Scheduling.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.Triggers;

/// <summary>
/// A root <see cref="BpmnProcess"/> started by one of its own triggers begins at the event-defined start event that
/// trigger registered for, rather than at its plain start events or at none at all.
/// </summary>
/// <remarks>
/// <para>
/// Message and signal starts go end to end: the workflow is registered, its triggers are indexed, and an ordinary
/// <see cref="IEventPublisher.PublishAsync"/> starts it. A timer start that has to be told apart from another goes
/// through the real trigger scheduler, and the schedule it registers is fired as the <see cref="RunWorkflowTask"/> it
/// would run when it elapses, since waiting for a real schedule would only make the test slow.
/// </para>
/// <para>
/// The remaining runs go straight to the workflow runner with the process as the trigger activity. They carry no
/// trigger hash, which is how the kind-based fallback, and its ambiguity fault, are exercised.
/// </para>
/// <para>
/// Each process routes its start events to different work so the start the instance began at is observable from the
/// workflow's status alone: a path to blocking work leaves it suspended, a path straight to an end finishes it.
/// </para>
/// </remarks>
public class BpmnTriggeredStartTests(ITestOutputHelper testOutputHelper)
{
    [Fact(DisplayName = "Publishing the message of a message start starts the process at that start")]
    public async Task MessageStart_PublishingTheMessage_StartsAndFinishes()
    {
        var instance = await PublishAsync<MessageStartWorkflow>("OrderPlaced");

        AssertFinished(instance);
    }

    [Fact(DisplayName = "Publishing the signal of a signal start starts the process at that start")]
    public async Task SignalStart_PublishingTheSignal_StartsAndFinishes()
    {
        var instance = await PublishAsync<SignalStartWorkflow>("OrderCancelled");

        AssertFinished(instance);
    }

    [Fact(DisplayName = "Firing a timer start starts the process at that start")]
    public async Task TimerStart_FiringTheTimer_StartsAndFinishes()
    {
        var result = await RunAsync(new TimerStartWorkflow(), triggeredByTimer: true);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A message trigger starts a process that also has a plain start at the message start only")]
    public async Task PlainAndMessageStart_PublishingTheMessage_StartsAtTheMessageStart()
    {
        // The plain start leads to blocking work; starting there as well, or instead, would leave the instance suspended.
        var instance = await PublishAsync<PlainAndMessageStartWorkflow>("OrderPlaced");

        AssertFinished(instance);
    }

    [Fact(DisplayName = "Running a process that also has a message start directly still starts it at its plain start")]
    public async Task PlainAndMessageStart_RunDirectly_StartsAtThePlainStart()
    {
        var result = await RunAsync(new PlainAndMessageStartWorkflow(), triggeredByTimer: false);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Suspended, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A message trigger starts a process with message and timer starts at the message start")]
    public async Task MessageAndTimerStart_PublishingTheMessage_StartsAtTheMessageStart()
    {
        var instance = await PublishAsync<MessageAndTimerStartWorkflow>("OrderPlaced");

        AssertFinished(instance);
    }

    [Fact(DisplayName = "A timer trigger starts a process with message and timer starts at the timer start")]
    public async Task MessageAndTimerStart_FiringTheTimer_StartsAtTheTimerStart()
    {
        // The timer start leads to blocking work, the message start straight to an end.
        var result = await RunAsync(new MessageAndTimerStartWorkflow(), triggeredByTimer: true);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Suspended, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "Two message starts on the same name are one trigger, so publishing it starts the process once")]
    public async Task TwoMessageStartsOnTheSameName_PublishingTheMessage_StartsAndFinishes()
    {
        var instance = await PublishAsync<TwoMessageStartsOnTheSameNameWorkflow>("OrderPlaced");

        AssertFinished(instance);
    }

    [Fact(DisplayName = "Publishing the first of two message names starts the process at the start that registered it")]
    public async Task TwoMessageStartsOnDifferentNames_PublishingTheFirst_StartsAtItsStart()
    {
        // The placed start leads straight to an end, the cancelled start to blocking work.
        var instance = await PublishAsync<TwoMessageStartsOnDifferentNamesWorkflow>("OrderPlaced");

        AssertFinished(instance);
    }

    [Fact(DisplayName = "Publishing the second of two message names starts the process at the start that registered it")]
    public async Task TwoMessageStartsOnDifferentNames_PublishingTheSecond_StartsAtItsStart()
    {
        var instance = await PublishAsync<TwoMessageStartsOnDifferentNamesWorkflow>("OrderCancelled");

        Assert.Empty(instance.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
    }

    [Fact(DisplayName = "A run with no trigger hash on a process whose message starts register different names faults instead of guessing")]
    public async Task TwoMessageStartsOnDifferentNames_RunAsTriggerWithoutHash_FaultsAsAmbiguous()
    {
        var result = await RunAsync(new TwoMessageStartsOnDifferentNamesWorkflow(), TriggeredBy.EventWithoutHash);

        AssertAmbiguous(result);
    }

    [Fact(DisplayName = "Firing the shorter of two timer schedules starts the process at the start that registered it")]
    public async Task TwoTimerStartsOnDifferentIntervals_FiringTheShorter_StartsAtItsStart()
    {
        // The hourly start leads straight to an end, the two-hourly start to blocking work.
        var instance = await FireScheduledTimerAsync<TwoTimerStartsOnDifferentIntervalsWorkflow>(TimeSpan.FromHours(1));

        AssertFinished(instance);
    }

    [Fact(DisplayName = "Firing the longer of two timer schedules starts the process at the start that registered it")]
    public async Task TwoTimerStartsOnDifferentIntervals_FiringTheLonger_StartsAtItsStart()
    {
        var instance = await FireScheduledTimerAsync<TwoTimerStartsOnDifferentIntervalsWorkflow>(TimeSpan.FromHours(2));

        Assert.Empty(instance.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
    }

    [Fact(DisplayName = "A timer run with no trigger hash on a process whose timer starts register different intervals faults instead of guessing")]
    public async Task TwoTimerStartsOnDifferentIntervals_RunAsTriggerWithoutHash_FaultsAsAmbiguous()
    {
        var result = await RunAsync(new TwoTimerStartsOnDifferentIntervalsWorkflow(), TriggeredBy.TimerWithoutHash);

        AssertAmbiguous(result);
    }

    private async Task<WorkflowInstance> PublishAsync<TWorkflow>(string eventName) where TWorkflow : IWorkflow
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmn())
            .AddActivitiesFrom<BpmnTestWork>()
            .AddWorkflow<TWorkflow>()
            .Build();

        await services.PopulateRegistriesAsync();

        var correlationId = Guid.NewGuid().ToString();
        await services.GetRequiredService<IEventPublisher>().PublishAsync(eventName, correlationId);

        var instances = await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter
        {
            DefinitionId = typeof(TWorkflow).Name,
            CorrelationId = correlationId
        });

        return Assert.Single(instances);
    }

    /// <summary>
    /// Schedules the workflow's stored triggers through the real <see cref="ITriggerScheduler"/>, then fires the timer
    /// schedule with the given interval the way the scheduler does when it elapses: as a <see cref="RunWorkflowTask"/>.
    /// </summary>
    private async Task<WorkflowInstance> FireScheduledTimerAsync<TWorkflow>(TimeSpan interval) where TWorkflow : IWorkflow
    {
        var workflowScheduler = new CapturingWorkflowScheduler();
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmn().UseScheduling(scheduling => scheduling.WorkflowScheduler = _ => workflowScheduler))
            .AddActivitiesFrom<BpmnTestWork>()
            .AddWorkflow<TWorkflow>()
            .Build();

        await services.PopulateRegistriesAsync();

        var triggers = await services.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { WorkflowDefinitionId = typeof(TWorkflow).Name });
        await services.GetRequiredService<ITriggerScheduler>().ScheduleAsync(triggers);

        var request = Assert.Single(workflowScheduler.RecurringSchedules, schedule => schedule.Interval == interval).Request;
        await new RunWorkflowTask(request).ExecuteAsync(new TaskExecutionContext(services, CancellationToken.None));

        var instances = await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter
        {
            DefinitionId = typeof(TWorkflow).Name
        });

        return Assert.Single(instances);
    }

    private async Task<RunWorkflowResult> RunAsync(IWorkflow workflowDefinition, bool triggeredByTimer) =>
        await RunAsync(workflowDefinition, triggeredByTimer ? TriggeredBy.TimerWithoutHash : TriggeredBy.Nothing);

    /// <summary>
    /// Runs the workflow directly, never through the runtime, so a trigger-started run carries no trigger hash.
    /// </summary>
    private async Task<RunWorkflowResult> RunAsync(IWorkflow workflowDefinition, TriggeredBy triggeredBy)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmn())
            .AddActivitiesFrom<BpmnTestWork>()
            .Build();

        await services.PopulateRegistriesAsync();

        var workflow = await services.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder().BuildWorkflowAsync(workflowDefinition);
        var options = new RunWorkflowOptions
        {
            TriggerActivityId = triggeredBy == TriggeredBy.Nothing ? null : ProcessId,
            Input = triggeredBy == TriggeredBy.EventWithoutHash
                ? new Dictionary<string, object> { [Event.EventInputWorkflowInputKey] = "payload" }
                : null
        };

        return await services.GetRequiredService<IWorkflowRunner>().RunAsync(workflow, options);
    }

    private static void AssertFinished(WorkflowInstance instance)
    {
        Assert.Empty(instance.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
    }

    private static void AssertAmbiguous(RunWorkflowResult result)
    {
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);
        var incident = Assert.Single(result.WorkflowState.Incidents);
        Assert.Contains(BpmnStartEventResolver.AmbiguousTriggerFaultCode, incident.Message);
    }

    private enum TriggeredBy
    {
        Nothing,
        EventWithoutHash,
        TimerWithoutHash
    }

    /// <summary>Records the recurring schedules the trigger scheduler asks for instead of waiting for them to elapse.</summary>
    private sealed class CapturingWorkflowScheduler : IWorkflowScheduler
    {
        // Keyed by task name, the stored trigger's id, so a trigger scheduled again replaces its earlier schedule.
        private readonly Dictionary<string, (ScheduleNewWorkflowInstanceRequest Request, TimeSpan Interval)> _recurringSchedules = new();

        public IEnumerable<(ScheduleNewWorkflowInstanceRequest Request, TimeSpan Interval)> RecurringSchedules => _recurringSchedules.Values;

        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            _recurringSchedules[taskName] = (request, interval);
            return default;
        }

        public ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => default;
        public ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default) => default;
    }

    private const string ProcessId = "process";

    private static BpmnProcess RootScope(BpmnProcessDefinition definition, params IActivity[] work) => new()
    {
        Id = ProcessId,
        IsRootScope = true,
        Process = definition,
        Activities = work.ToList(),
        WorkBindings = work.ToDictionary(activity => BindingRef(activity.Id), activity => activity.Id, StringComparer.Ordinal)
    };

    private static string BindingRef(string elementId) => $"node-{elementId}";

    private static BpmnTestWork Immediate(string id) => new() { Id = id };

    private static BpmnTestBlockingWork Blocking(string id) => new() { Id = id };

    private static BpmnEventDefinition Message(string name) =>
        new(BpmnEventDefinitionTypes.Message, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Name] = name });

    private static BpmnEventDefinition Signal(string name) =>
        new(BpmnEventDefinitionTypes.Signal, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Name] = name });

    private static BpmnEventDefinition TimerInterval(string isoDuration) =>
        new(BpmnEventDefinitionTypes.Timer, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Interval] = isoDuration });

    private sealed class MessageStartWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(MessageStartWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("start", null, Message("OrderPlaced"))
                    .Task("handle", null, BindingRef("handle"))
                    .EndEvent("end")
                    .ConnectSequence("start", "handle", "end")
                    .Build(),
                Immediate("handle"));
        }
    }

    private sealed class SignalStartWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(SignalStartWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("start", null, Signal("OrderCancelled"))
                    .Task("handle", null, BindingRef("handle"))
                    .EndEvent("end")
                    .ConnectSequence("start", "handle", "end")
                    .Build(),
                Immediate("handle"));
        }
    }

    private sealed class TimerStartWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(TimerStartWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("start", null, TimerInterval("PT1H"))
                    .Task("handle", null, BindingRef("handle"))
                    .EndEvent("end")
                    .ConnectSequence("start", "handle", "end")
                    .Build(),
                Immediate("handle"));
        }
    }

    private sealed class PlainAndMessageStartWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(PlainAndMessageStartWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("plainStart")
                    .Task("wait", null, BindingRef("wait"))
                    .EndEvent("waited")
                    .StartEvent("messageStart", null, Message("OrderPlaced"))
                    .EndEvent("end")
                    .ConnectSequence("plainStart", "wait", "waited")
                    .ConnectSequence("messageStart", "end")
                    .Build(),
                Blocking("wait"));
        }
    }

    private sealed class MessageAndTimerStartWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(MessageAndTimerStartWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("messageStart", null, Message("OrderPlaced"))
                    .EndEvent("end")
                    .StartEvent("timerStart", null, TimerInterval("PT1H"))
                    .Task("wait", null, BindingRef("wait"))
                    .EndEvent("waited")
                    .ConnectSequence("messageStart", "end")
                    .ConnectSequence("timerStart", "wait", "waited")
                    .Build(),
                Blocking("wait"));
        }
    }

    private sealed class TwoMessageStartsOnTheSameNameWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(TwoMessageStartsOnTheSameNameWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("firstStart", null, Message("OrderPlaced"))
                    .EndEvent("firstEnd")
                    .StartEvent("secondStart", null, Message("OrderPlaced"))
                    .EndEvent("secondEnd")
                    .ConnectSequence("firstStart", "firstEnd")
                    .ConnectSequence("secondStart", "secondEnd")
                    .Build());
        }
    }

    private sealed class TwoMessageStartsOnDifferentNamesWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(TwoMessageStartsOnDifferentNamesWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("placedStart", null, Message("OrderPlaced"))
                    .EndEvent("placedEnd")
                    .StartEvent("cancelledStart", null, Message("OrderCancelled"))
                    .Task("wait", null, BindingRef("wait"))
                    .EndEvent("cancelledEnd")
                    .ConnectSequence("placedStart", "placedEnd")
                    .ConnectSequence("cancelledStart", "wait", "cancelledEnd")
                    .Build(),
                Blocking("wait"));
        }
    }

    private sealed class TwoTimerStartsOnDifferentIntervalsWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(TwoTimerStartsOnDifferentIntervalsWorkflow));
            builder.Root = RootScope(
                new BpmnProcessBuilder(ProcessId)
                    .StartEvent("hourlyStart", null, TimerInterval("PT1H"))
                    .EndEvent("hourlyEnd")
                    .StartEvent("twoHourlyStart", null, TimerInterval("PT2H"))
                    .Task("wait", null, BindingRef("wait"))
                    .EndEvent("twoHourlyEnd")
                    .ConnectSequence("hourlyStart", "hourlyEnd")
                    .ConnectSequence("twoHourlyStart", "wait", "twoHourlyEnd")
                    .Build(),
                Blocking("wait"));
        }
    }
}

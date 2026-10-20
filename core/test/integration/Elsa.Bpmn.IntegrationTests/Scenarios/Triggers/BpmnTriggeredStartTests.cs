using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
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
/// <see cref="IEventPublisher.PublishAsync"/> starts it. A timer start is started the way <c>Elsa.Scheduling</c>'s
/// <c>RunWorkflowTask</c> starts one, with the process as the trigger activity and no input, since waiting for a real
/// schedule to elapse would only make the test slow.
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

    [Fact(DisplayName = "A process whose message starts register different names faults instead of guessing which one fired")]
    public async Task TwoMessageStartsOnDifferentNames_PublishingOneOfThem_FaultsAsAmbiguous()
    {
        var instance = await PublishAsync<TwoMessageStartsOnDifferentNamesWorkflow>("OrderPlaced");

        Assert.Equal(WorkflowSubStatus.Faulted, instance.SubStatus);
        var incident = Assert.Single(instance.WorkflowState.Incidents);
        Assert.Contains(BpmnStartEventResolver.AmbiguousTriggerFaultCode, incident.Message);
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

    private async Task<RunWorkflowResult> RunAsync(IWorkflow workflowDefinition, bool triggeredByTimer)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmn())
            .AddActivitiesFrom<BpmnTestWork>()
            .Build();

        await services.PopulateRegistriesAsync();

        var workflow = await services.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder().BuildWorkflowAsync(workflowDefinition);
        var options = new RunWorkflowOptions
        {
            TriggerActivityId = triggeredByTimer ? ProcessId : null
        };

        return await services.GetRequiredService<IWorkflowRunner>().RunAsync(workflow, options);
    }

    private static void AssertFinished(WorkflowInstance instance)
    {
        Assert.Empty(instance.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
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
                    .EndEvent("cancelledEnd")
                    .ConnectSequence("placedStart", "placedEnd")
                    .ConnectSequence("cancelledStart", "cancelledEnd")
                    .Build());
        }
    }
}

using System.Text.Json;
using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Scheduling.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stimuli;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerStimulusHash;

/// <summary>
/// A workflow the runtime starts from a stored trigger can read which trigger that was
/// (<see cref="TriggerWorkflowProperties.StimulusHash"/>), whether an event or a timer schedule fired it.
/// </summary>
public class Tests(ITestOutputHelper testOutputHelper)
{
    private const string EventName = "TriggerStimulusHashPing";

    [Fact(DisplayName = "An event-started workflow reads the hash of the event trigger that started it")]
    public async Task EventTrigger_StartsTheWorkflowWithItsHash()
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .AddActivitiesFrom<RecordTriggerStimulusHash>()
            .AddWorkflow<EventStartedWorkflow>()
            .Build();
        await services.PopulateRegistriesAsync();
        var correlationId = Guid.NewGuid().ToString();

        await services.GetRequiredService<IEventPublisher>().PublishAsync(EventName, correlationId);

        var instance = await FindSingleInstanceAsync(services, nameof(EventStartedWorkflow));
        var expectedHash = services.GetRequiredService<IStimulusHasher>().Hash(RuntimeStimulusNames.Event, new EventStimulus(EventName));
        Assert.Equal(await FindTriggerHashAsync(services, nameof(EventStartedWorkflow)), expectedHash);
        AssertStartedWith(instance, expectedHash);
    }

    [Theory(DisplayName = "A timer-started workflow reads the hash of the timer trigger that started it")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TimerTrigger_StartsTheWorkflowWithItsHash(bool throughJsonJobData)
    {
        var workflowScheduler = new CapturingWorkflowScheduler();
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseScheduling(scheduling => scheduling.WorkflowScheduler = _ => workflowScheduler))
            .AddActivitiesFrom<RecordTriggerStimulusHash>()
            .AddWorkflow<TimerStartedWorkflow>()
            .Build();
        await services.PopulateRegistriesAsync();
        var triggers = await services.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { WorkflowDefinitionId = nameof(TimerStartedWorkflow) });
        await services.GetRequiredService<ITriggerScheduler>().ScheduleAsync(triggers);
        var request = Assert.Single(workflowScheduler.RecurringSchedules);

        // A scheduler that persists its job data as JSON (Quartz) hands the properties back as JSON elements.
        if (throughJsonJobData)
            request.Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Properties));

        await new RunWorkflowTask(request).ExecuteAsync(new(services, CancellationToken.None));

        var instance = await FindSingleInstanceAsync(services, nameof(TimerStartedWorkflow));
        AssertStartedWith(instance, await FindTriggerHashAsync(services, nameof(TimerStartedWorkflow)));
    }

    private static void AssertStartedWith(WorkflowInstance instance, string expectedHash)
    {
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
        Assert.Equal(expectedHash, instance.WorkflowState.Output[RecordTriggerStimulusHash.OutputKey]);
    }

    private static async Task<WorkflowInstance> FindSingleInstanceAsync(IServiceProvider services, string definitionId)
    {
        var instances = await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter { DefinitionId = definitionId });
        return Assert.Single(instances);
    }

    private static async Task<string> FindTriggerHashAsync(IServiceProvider services, string definitionId)
    {
        var triggers = await services.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { WorkflowDefinitionId = definitionId });
        return Assert.Single(triggers).Hash!;
    }

    private sealed class EventStartedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(EventStartedWorkflow));
            builder.Root = new Sequence
            {
                Activities =
                {
                    new Event(EventName) { CanStartWorkflow = true },
                    new RecordTriggerStimulusHash()
                }
            };
        }
    }

    private sealed class TimerStartedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(nameof(TimerStartedWorkflow));
            builder.Root = new Sequence
            {
                Activities =
                {
                    new Timer(TimeSpan.FromHours(1)) { CanStartWorkflow = true },
                    new RecordTriggerStimulusHash()
                }
            };
        }
    }

    /// <summary>Records the stored trigger hash the runtime reports into the workflow output.</summary>
    public class RecordTriggerStimulusHash : CodeActivity
    {
        public const string OutputKey = "TriggerStimulusHash";

        protected override void Execute(ActivityExecutionContext context) =>
            context.WorkflowExecutionContext.Output[OutputKey] = context.WorkflowExecutionContext.GetTriggerStimulusHash() ?? "none";
    }

    /// <summary>Records the recurring schedules the trigger scheduler asks for instead of waiting for them to elapse.</summary>
    private sealed class CapturingWorkflowScheduler : IWorkflowScheduler
    {
        // Keyed by task name, the stored trigger's id, so a trigger scheduled again replaces its earlier schedule.
        private readonly Dictionary<string, ScheduleNewWorkflowInstanceRequest> _recurringSchedules = new();

        public IEnumerable<ScheduleNewWorkflowInstanceRequest> RecurringSchedules => _recurringSchedules.Values;

        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            _recurringSchedules[taskName] = request;
            return default;
        }

        public ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => default;
        public ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => default;
        public ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default) => default;
    }
}

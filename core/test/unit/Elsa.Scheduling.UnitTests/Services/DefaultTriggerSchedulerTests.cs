using Elsa.Common;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Scheduling.Services;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.Services;

public class DefaultTriggerSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_SchedulesPastDueStartAtTriggerForCatchUp()
    {
        var workflowScheduler = Substitute.For<IWorkflowScheduler>();
        var systemClock = Substitute.For<ISystemClock>();
        var logger = Substitute.For<ILogger<DefaultTriggerScheduler>>();
        var scheduler = new DefaultTriggerScheduler(workflowScheduler, systemClock, logger);
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, TimeSpan.Zero);
        var executeAt = now.AddMinutes(-5);
        ScheduleNewWorkflowInstanceRequest? scheduledRequest = null;
        var trigger = new StoredTrigger
        {
            Id = "trigger-1",
            Name = ActivityTypeNameHelper.GenerateTypeName<StartAt>(),
            WorkflowDefinitionVersionId = "workflow-version",
            ActivityId = "activity-1",
            Payload = new StartAtPayload(executeAt)
        };
        systemClock.UtcNow.Returns(now);
        workflowScheduler.ScheduleAtAsync(trigger.Id, Arg.Do<ScheduleNewWorkflowInstanceRequest>(x => scheduledRequest = x), executeAt, Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        await scheduler.ScheduleAsync([trigger], CancellationToken.None);

        await workflowScheduler.Received(1).ScheduleAtAsync(trigger.Id, Arg.Any<ScheduleNewWorkflowInstanceRequest>(), executeAt, Arg.Any<CancellationToken>());
        Assert.NotNull(scheduledRequest);
        Assert.Equal(trigger.ActivityId, scheduledRequest.TriggerActivityId);
        Assert.Equal(trigger.WorkflowDefinitionVersionId, scheduledRequest.WorkflowDefinitionHandle.DefinitionVersionId);
    }

    [Fact]
    public async Task ScheduleAsync_CarriesEachTriggersStimulusHashInTheScheduledRequest()
    {
        var workflowScheduler = Substitute.For<IWorkflowScheduler>();
        var systemClock = Substitute.For<ISystemClock>();
        var logger = Substitute.For<ILogger<DefaultTriggerScheduler>>();
        var scheduler = new DefaultTriggerScheduler(workflowScheduler, systemClock, logger);
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, TimeSpan.Zero);
        var requests = new Dictionary<string, ScheduleNewWorkflowInstanceRequest>();
        var timerTrigger = CreateTrigger<Activities.Timer>("timer", new TimerTriggerPayload(now.AddHours(1), TimeSpan.FromHours(1)));
        var startAtTrigger = CreateTrigger<StartAt>("start-at", new StartAtPayload(now.AddHours(1)));
        var cronTrigger = CreateTrigger<Cron>("cron", new CronTriggerPayload("0 0 * * * ?"));
        systemClock.UtcNow.Returns(now);
        workflowScheduler
            .ScheduleRecurringAsync(Arg.Any<string>(), Arg.Any<ScheduleNewWorkflowInstanceRequest>(), Arg.Any<DateTimeOffset>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(call => requests[call.ArgAt<string>(0)] = call.ArgAt<ScheduleNewWorkflowInstanceRequest>(1));
        workflowScheduler
            .ScheduleAtAsync(Arg.Any<string>(), Arg.Any<ScheduleNewWorkflowInstanceRequest>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(call => requests[call.ArgAt<string>(0)] = call.ArgAt<ScheduleNewWorkflowInstanceRequest>(1));
        workflowScheduler
            .ScheduleCronAsync(Arg.Any<string>(), Arg.Any<ScheduleNewWorkflowInstanceRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(call => requests[call.ArgAt<string>(0)] = call.ArgAt<ScheduleNewWorkflowInstanceRequest>(1));

        await scheduler.ScheduleAsync([timerTrigger, startAtTrigger, cronTrigger], CancellationToken.None);

        Assert.Equal(3, requests.Count);
        foreach (var trigger in new[] { timerTrigger, startAtTrigger, cronTrigger })
        {
            var properties = requests[trigger.Id].Properties;
            Assert.NotNull(properties);
            Assert.Equal(trigger.Hash, properties[TriggerWorkflowProperties.StimulusHash]);
        }
    }

    private static StoredTrigger CreateTrigger<TActivity>(string id, object payload) where TActivity : IActivity => new()
    {
        Id = id,
        Name = ActivityTypeNameHelper.GenerateTypeName<TActivity>(),
        WorkflowDefinitionVersionId = "workflow-version",
        ActivityId = $"activity-{id}",
        Hash = $"hash-{id}",
        Payload = payload
    };
}

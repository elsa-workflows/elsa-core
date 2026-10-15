using Elsa.Common;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Scheduling.Services;
using Elsa.Workflows.Helpers;
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
}

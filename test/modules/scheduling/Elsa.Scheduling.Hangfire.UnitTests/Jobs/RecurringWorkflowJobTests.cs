using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Hangfire.Jobs;
using Elsa.Scheduling;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage.Monitoring;
using Moq;

namespace Elsa.Scheduling.Hangfire.UnitTests.Jobs;

/// <summary>
/// Interval jobs arm the next fire at <c>scheduledAt + interval</c> (Quartz simple-schedule / Local recurring semantics)
/// before running the current occurrence.
/// </summary>
public class RecurringWorkflowJobTests
{
    [Fact]
    public async Task ExecuteRecurringAsync_RunWorkflowJob_SchedulesNextOccurrenceAtStartAtPlusInterval()
    {
        var storage = new MemoryStorage();
        var job = CreateRunJob(storage, out var workflowClient);
        var scheduledAt = DateTimeOffset.UtcNow.AddHours(1);
        var interval = TimeSpan.FromHours(1);
        var request = new ScheduleNewWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId("definition-version-1"),
            TriggerActivityId = "activity-1"
        };

        await job.ExecuteRecurringAsync("task-1", request, null, scheduledAt, interval, CancellationToken.None);

        var next = Assert.Single(GetScheduledJobs(storage, "task-1"));
        Assert.Equal(nameof(RunWorkflowJob.ExecuteRecurringAsync), next.Job.Method.Name);
        Assert.Equal(scheduledAt + interval, (DateTimeOffset)next.Job.Args[3]);
        Assert.Equal(interval, (TimeSpan)next.Job.Args[4]);
        Assert.Equal((scheduledAt + interval).UtcDateTime, next.EnqueueAt, TimeSpan.FromSeconds(1));
        workflowClient.Verify(c => c.CreateAndRunInstanceAsync(It.IsAny<CreateAndRunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteRecurringAsync_ResumeWorkflowJob_SchedulesNextOccurrenceAtStartAtPlusInterval()
    {
        var storage = new MemoryStorage();
        var job = CreateResumeJob(storage, out var workflowClient);
        var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var interval = TimeSpan.FromMinutes(30);
        var request = new ScheduleExistingWorkflowInstanceRequest
        {
            WorkflowInstanceId = "instance-1"
        };

        await job.ExecuteRecurringAsync("task-1", request, null, scheduledAt, interval, CancellationToken.None);

        var next = Assert.Single(GetScheduledJobs(storage, "task-1"));
        Assert.Equal(nameof(ResumeWorkflowJob.ExecuteRecurringAsync), next.Job.Method.Name);
        Assert.Equal(scheduledAt + interval, (DateTimeOffset)next.Job.Args[3]);
        Assert.Equal(interval, (TimeSpan)next.Job.Args[4]);
        Assert.Equal((scheduledAt + interval).UtcDateTime, next.EnqueueAt, TimeSpan.FromSeconds(1));
        workflowClient.Verify(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteRecurringAsync_RunWorkflowJob_StillArmsNextOccurrenceWhenExecutionFails()
    {
        var storage = new MemoryStorage();
        var job = CreateRunJob(storage, out var workflowClient, succeed: false);
        var scheduledAt = DateTimeOffset.UtcNow.AddHours(2);
        var interval = TimeSpan.FromHours(24);
        var request = new ScheduleNewWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId("definition-version-1")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteRecurringAsync("task-1", request, null, scheduledAt, interval, CancellationToken.None));

        var next = Assert.Single(GetScheduledJobs(storage, "task-1"));
        Assert.Equal(interval, (TimeSpan)next.Job.Args[4]);
        Assert.Equal((scheduledAt + interval).UtcDateTime, next.EnqueueAt, TimeSpan.FromSeconds(1));
        workflowClient.Verify(c => c.CreateAndRunInstanceAsync(It.IsAny<CreateAndRunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RunWorkflowJob CreateRunJob(JobStorage storage, out Mock<IWorkflowClient> workflowClient, bool succeed = true)
    {
        workflowClient = new Mock<IWorkflowClient>();
        var create = workflowClient.Setup(c => c.CreateAndRunInstanceAsync(It.IsAny<CreateAndRunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()));
        if (succeed)
            create.ReturnsAsync(new RunWorkflowInstanceResponse());
        else
            create.ThrowsAsync(new InvalidOperationException("workflow failed"));

        var runtime = new Mock<IWorkflowRuntime>();
        runtime.Setup(r => r.CreateClientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(workflowClient.Object);

        return new RunWorkflowJob(runtime.Object, new Mock<ITenantFinder>().Object, CreateTenantAccessor().Object, new BackgroundJobClient(storage));
    }

    private static ResumeWorkflowJob CreateResumeJob(JobStorage storage, out Mock<IWorkflowClient> workflowClient)
    {
        workflowClient = new Mock<IWorkflowClient>();
        workflowClient.Setup(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunWorkflowInstanceResponse());

        var runtime = new Mock<IWorkflowRuntime>();
        runtime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(workflowClient.Object);

        return new ResumeWorkflowJob(runtime.Object, new Mock<ITenantFinder>().Object, CreateTenantAccessor().Object, new BackgroundJobClient(storage));
    }

    private static Mock<ITenantAccessor> CreateTenantAccessor()
    {
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.PushContext(It.IsAny<Tenant>())).Returns(Mock.Of<IDisposable>());
        return tenantAccessor;
    }

    private static IReadOnlyList<ScheduledJobDto> GetScheduledJobs(JobStorage storage, string taskName)
    {
        return storage.GetMonitoringApi()
            .ScheduledJobs(0, 100)
            .Where(job => job.Value.Job?.Args is { Count: > 0 } && (string)job.Value.Job.Args[0] == taskName)
            .Select(job => job.Value)
            .ToList();
    }
}

using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Jobs;

public class ExecuteBackgroundActivityJobTests
{
    private readonly Mock<IBackgroundActivityInvoker> _invoker = new();
    private readonly Mock<ILogger<ExecuteBackgroundActivityJob>> _logger = new();
    private readonly ExecuteBackgroundActivityJob _job;

    public ExecuteBackgroundActivityJobTests()
    {
        _job = new(_invoker.Object, Mock.Of<ITenantFinder>(), Mock.Of<ITenantAccessor>(), _logger.Object);
    }

    [Fact]
    public async Task Execute_InvokesBackgroundActivityAndDeletesJob()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        ScheduledBackgroundActivity? captured = null;
        _invoker.Setup(i => i.ExecuteAsync(It.IsAny<ScheduledBackgroundActivity>(), It.IsAny<CancellationToken>()))
            .Callback<ScheduledBackgroundActivity, CancellationToken>((activity, _) => captured = activity)
            .Returns(Task.CompletedTask);

        await _job.Execute(context);

        Assert.NotNull(captured);
        Assert.Equal("instance-1", captured.WorkflowInstanceId);
        Assert.Equal("node-1", captured.ActivityNodeId);
        Assert.Equal("bookmark-1", captured.BookmarkId);
        scheduler.VerifyDeleted();
    }

    [Fact]
    public async Task ExecuteBackgroundActivityJob_WithoutTenantAwareFactory_SeesTriggerTenantDuringExecute()
    {
        var accessor = new DefaultTenantAccessor();
        string? seen = "unset";
        var invoker = new Mock<IBackgroundActivityInvoker>();
        invoker.Setup(i => i.ExecuteAsync(It.IsAny<ScheduledBackgroundActivity>(), It.IsAny<CancellationToken>()))
            .Callback(() => seen = accessor.Tenant?.Id)
            .Returns(Task.CompletedTask);
        var job = new ExecuteBackgroundActivityJob(invoker.Object, new AnyIdTenantFinder(), accessor, Mock.Of<ILogger<ExecuteBackgroundActivityJob>>());
        var (context, scheduler) = QuartzJobTestHelper.CreateJobExecutionContext(
            new Dictionary<string, object>
            {
                [nameof(ScheduledBackgroundActivity.WorkflowInstanceId)] = "instance-1",
                [nameof(ScheduledBackgroundActivity.ActivityNodeId)] = "node-1",
                [nameof(ScheduledBackgroundActivity.BookmarkId)] = "bookmark-1"
            },
            triggerData: new Dictionary<string, object> { ["TenantId"] = "tenant-b" });

        await job.Execute(context);

        Assert.Equal("tenant-b", seen);
        scheduler.VerifyDeleted();
    }

    [Fact]
    public async Task Execute_WhenInvokerFails_LogsDeletesAndRethrows()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _invoker.Setup(i => i.ExecuteAsync(It.IsAny<ScheduledBackgroundActivity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.Execute(context));

        _logger.VerifyLogged(LogLevel.Error, Times.Once());
        scheduler.VerifyDeleted();
    }

    private static (IJobExecutionContext Context, Mock<global::Quartz.IScheduler> Scheduler) CreateJobExecutionContext()
    {
        var jobData = new Dictionary<string, object>
        {
            [nameof(ScheduledBackgroundActivity.WorkflowInstanceId)] = "instance-1",
            [nameof(ScheduledBackgroundActivity.ActivityNodeId)] = "node-1",
            [nameof(ScheduledBackgroundActivity.BookmarkId)] = "bookmark-1"
        };

        return QuartzJobTestHelper.CreateJobExecutionContext(jobData);
    }
}

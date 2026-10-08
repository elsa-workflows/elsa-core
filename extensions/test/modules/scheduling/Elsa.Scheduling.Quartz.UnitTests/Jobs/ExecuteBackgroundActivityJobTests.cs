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
    private readonly Mock<ITenantFinder> _tenantFinder = new();
    private readonly Mock<ITenantAccessor> _tenantAccessor = QuartzJobTestHelper.CreateTenantAccessor();
    private readonly Mock<ILogger<ExecuteBackgroundActivityJob>> _logger = new();
    private readonly ExecuteBackgroundActivityJob _job;

    public ExecuteBackgroundActivityJobTests()
    {
        _job = new(_invoker.Object, _tenantFinder.Object, _tenantAccessor.Object, _logger.Object);
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
        _tenantAccessor.Verify(t => t.PushContext(null), Times.Once);
        scheduler.VerifyDeleted();
    }

    [Fact]
    public async Task Execute_ResolvesTenantBeforeInvoking()
    {
        var tenant = new Tenant { Id = "tenant-123" };
        _tenantFinder.Setup(f => f.FindByIdAsync("tenant-123", It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        var (context, _) = CreateJobExecutionContext(withTenantId: true);

        await _job.Execute(context);

        _tenantFinder.Verify(f => f.FindByIdAsync("tenant-123", It.IsAny<CancellationToken>()), Times.Once);
        _tenantAccessor.Verify(t => t.PushContext(tenant), Times.Once);
        _invoker.Verify(i => i.ExecuteAsync(It.IsAny<ScheduledBackgroundActivity>(), It.IsAny<CancellationToken>()), Times.Once);
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

    private static (IJobExecutionContext Context, Mock<global::Quartz.IScheduler> Scheduler) CreateJobExecutionContext(bool withTenantId = false)
    {
        var jobData = new Dictionary<string, object>
        {
            [nameof(ScheduledBackgroundActivity.WorkflowInstanceId)] = "instance-1",
            [nameof(ScheduledBackgroundActivity.ActivityNodeId)] = "node-1",
            [nameof(ScheduledBackgroundActivity.BookmarkId)] = "bookmark-1"
        };

        if (withTenantId)
            jobData["TenantId"] = "tenant-123";

        return QuartzJobTestHelper.CreateJobExecutionContext(jobData);
    }
}

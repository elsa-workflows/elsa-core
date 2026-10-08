using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class QuartzBackgroundActivitySchedulerTests
{
    [Fact]
    public async Task CreateAsync_AddsDurableJobWithoutTrigger()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _);
        var activity = CreateActivity();

        var jobId = await sut.CreateAsync(activity);

        Assert.False(string.IsNullOrWhiteSpace(jobId));
        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j =>
                j.Key.Name == jobId &&
                j.Key.Group == "Default" &&
                j.Durable &&
                j.JobType == typeof(ExecuteBackgroundActivityJob) &&
                (string)j.JobDataMap[nameof(ScheduledBackgroundActivity.WorkflowInstanceId)] == activity.WorkflowInstanceId &&
                (string)j.JobDataMap[nameof(ScheduledBackgroundActivity.ActivityNodeId)] == activity.ActivityNodeId &&
                (string)j.JobDataMap[nameof(ScheduledBackgroundActivity.BookmarkId)] == activity.BookmarkId),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RegistersJobInTenantSpecificGroup()
    {
        const string tenantId = "tenant-a";
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _, tenantId);

        var jobId = await sut.CreateAsync(CreateActivity());

        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j => j.Key.Name == jobId && j.Key.Group == tenantId && (string)j.JobDataMap["TenantId"] == tenantId),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_WhenJobExists_AddsStartNowTrigger()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ITrigger? capturedTrigger = null;
        scheduler.Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<ITrigger, CancellationToken>((trigger, _) => capturedTrigger = trigger)
            .ReturnsAsync(DateTimeOffset.UtcNow);
        var sut = CreateScheduler(scheduler, out _);

        await sut.ScheduleAsync("job-1");

        Assert.NotNull(capturedTrigger);
        Assert.Equal(new JobKey("job-1", "Default"), capturedTrigger!.JobKey);
        Assert.Equal(new TriggerKey("job-1", "Default"), capturedTrigger.Key);
        Assert.True(capturedTrigger.StartTimeUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ScheduleAsync_WhenJobMissing_DoesNotScheduleTrigger()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateScheduler(scheduler, out _);

        await sut.ScheduleAsync("missing-job");

        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleAsync_Activity_CreatesDurableJobThenSchedulesTrigger()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateScheduler(scheduler, out _);

        var jobId = await sut.ScheduleAsync(CreateActivity());

        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j => j.Key.Name == jobId && j.Durable),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnscheduledAsync_DeletesJob()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _);

        await sut.UnscheduledAsync("job-1");

        scheduler.Verify(s => s.DeleteJob(new JobKey("job-1", "Default"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_DeletesJob()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _);

        await sut.CancelAsync("job-1");

        scheduler.Verify(s => s.DeleteJob(new JobKey("job-1", "Default"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_WhenTriggerAlreadyExists_DoesNotThrow()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        scheduler.Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectAlreadyExistsException("trigger already exists"));
        var sut = CreateScheduler(scheduler, out _);

        await sut.ScheduleAsync("job-1");
    }

    [Fact]
    public async Task CreateAsync_WhenJobAlreadyExistsViaSqlStore_DoesNotThrow()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.AddJob(It.IsAny<IJobDetail>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobPersistenceException("job already exists", new ObjectAlreadyExistsException("duplicate")));
        var sut = CreateScheduler(scheduler, out _);

        await sut.CreateAsync(CreateActivity());
    }

    private static QuartzBackgroundActivityScheduler CreateScheduler(Mock<global::Quartz.IScheduler> scheduler, out Mock<ISchedulerFactory> factory, string? tenantId = null)
    {
        factory = new Mock<ISchedulerFactory>();
        factory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler.Object);

        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.Tenant).Returns(tenantId == null ? null : new Tenant { Id = tenantId });

        return new QuartzBackgroundActivityScheduler(
            factory.Object,
            tenantAccessor.Object,
            NullLogger<QuartzBackgroundActivityScheduler>.Instance);
    }

    private static ScheduledBackgroundActivity CreateActivity() => new("instance-1", "node-1", "bookmark-1");
}

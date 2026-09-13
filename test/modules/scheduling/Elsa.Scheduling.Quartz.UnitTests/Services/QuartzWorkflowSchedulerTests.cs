using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

/// <summary>
/// Tests that <see cref="QuartzWorkflowScheduler"/> registers the durable job referenced by a trigger
/// before scheduling it, so scheduling does not fail with "The job (...) referenced by the trigger does not exist".
/// </summary>
public class QuartzWorkflowSchedulerTests
{
    [Fact]
    public async Task ScheduleCronAsync_WhenJobMissing_RegistersDurableJobThenSchedulesTrigger()
    {
        // Arrange
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateScheduler(scheduler, out _);
        var request = CreateNewRequest();

        // Act
        await sut.ScheduleCronAsync("task-1", request, "0 0/5 * * * ?");

        // Assert: the durable RunWorkflowJob was added, then the trigger was scheduled.
        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j => j.Key.Name == nameof(RunWorkflowJob) && j.Durable),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleCronAsync_WhenJobAlreadyExists_DoesNotAddJob()
    {
        // Arrange
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateScheduler(scheduler, out _);
        var request = CreateNewRequest();

        // Act
        await sut.ScheduleCronAsync("task-1", request, "0 0/5 * * * ?");

        // Assert: no redundant registration, but the trigger is still scheduled.
        scheduler.Verify(s => s.AddJob(It.IsAny<IJobDetail>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleCronAsync_PersistsAScheduleGenerationOnTheOriginalTrigger()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ITrigger? capturedTrigger = null;
        scheduler.Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<ITrigger, CancellationToken>((trigger, _) => capturedTrigger = trigger)
            .ReturnsAsync(DateTimeOffset.UtcNow);

        var sut = CreateScheduler(scheduler, out _);

        await sut.ScheduleCronAsync("task-1", CreateNewRequest(), "0 0/5 * * * ?");

        Assert.NotNull(capturedTrigger);
        Assert.True(capturedTrigger!.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryScheduleGeneration, out var generation));
        Assert.NotNull(generation);
    }

    [Fact]
    public async Task ScheduleCronAsync_RegistersJobInTenantSpecificGroup()
    {
        // Arrange
        const string tenantId = "tenant-a";
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateScheduler(scheduler, out _, tenantId);
        var request = CreateNewRequest();

        // Act
        await sut.ScheduleCronAsync("task-1", request, "0 0/5 * * * ?");

        // Assert: the durable job is registered in the tenant's group, matching the trigger's job key.
        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j => j.Key.Group == tenantId && j.Key.Name == nameof(RunWorkflowJob)),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleRecurringAsync_ExistingInstance_RegistersResumeWorkflowJob()
    {
        // Arrange
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateScheduler(scheduler, out _);
        var request = new global::Elsa.Scheduling.ScheduleExistingWorkflowInstanceRequest { WorkflowInstanceId = "instance-1" };

        // Act
        await sut.ScheduleRecurringAsync("task-1", request, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        // Assert
        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j => j.Key.Name == nameof(ResumeWorkflowJob) && j.Durable),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleAtAsync_NewInstance_RegistersRunWorkflowJob()
    {
        // Arrange
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateScheduler(scheduler, out _);
        var request = CreateNewRequest();

        // Act
        await sut.ScheduleAtAsync("task-1", request, DateTimeOffset.UtcNow);

        // Assert
        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j => j.Key.Name == nameof(RunWorkflowJob) && j.Durable),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleAtAsync_ExistingInstance_RegistersResumeWorkflowJob()
    {
        // Arrange
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateScheduler(scheduler, out _);
        var request = new global::Elsa.Scheduling.ScheduleExistingWorkflowInstanceRequest { WorkflowInstanceId = "instance-1" };

        // Act
        await sut.ScheduleAtAsync("task-1", request, DateTimeOffset.UtcNow);

        // Assert
        scheduler.Verify(s => s.AddJob(
            It.Is<IJobDetail>(j => j.Key.Name == nameof(ResumeWorkflowJob) && j.Durable),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleCronAsync_WhenJobAddedConcurrently_SwallowsAlreadyExists()
    {
        // Arrange: CheckExists reports missing, but a concurrent instance registers it first, so AddJob throws.
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        scheduler.Setup(s => s.AddJob(It.IsAny<IJobDetail>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectAlreadyExistsException("job already exists"));

        var sut = CreateScheduler(scheduler, out _);
        var request = CreateNewRequest();

        // Act + Assert: does not throw, and still schedules the trigger.
        await sut.ScheduleCronAsync("task-1", request, "0 0/5 * * * ?");

        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleCronAsync_WhenJobAddedConcurrentlyViaSqlStore_SwallowsWrappedPersistenceException()
    {
        // Arrange: SQL-backed Quartz stores (e.g. AdoJobStore) wrap the duplicate-insert error in a
        // JobPersistenceException with an inner ObjectAlreadyExistsException rather than throwing the
        // inner exception directly. Verify that EnsureJobAsync handles this wrapping case.
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        scheduler.Setup(s => s.AddJob(It.IsAny<IJobDetail>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobPersistenceException("job already exists", new ObjectAlreadyExistsException("duplicate")));

        var sut = CreateScheduler(scheduler, out _);
        var request = CreateNewRequest();

        // Act + Assert: does not throw, and still schedules the trigger.
        await sut.ScheduleCronAsync("task-1", request, "0 0/5 * * * ?");

        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnscheduleAsync_RemovesTheTaskTriggerAndAnyPendingRetryTrigger()
    {
        // Arrange
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _);

        // Act
        await sut.UnscheduleAsync("task-1");

        // Assert: both the original schedule and its derived retry trigger are removed.
        scheduler.Verify(s => s.UnscheduleJob(It.Is<TriggerKey>(k => k.Name == "task-1" && k.Group == "Default"), It.IsAny<CancellationToken>()), Times.Once);
        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(new TriggerKey("task-1", "Default"));
        scheduler.Verify(s => s.UnscheduleJob(retryKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnscheduleAsync_TenantSpecificGroup_RemovesRetryTriggerFromTheSameGroup()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _, "tenant-a");

        await sut.UnscheduleAsync("task-1");

        scheduler.Verify(s => s.UnscheduleJob(It.Is<TriggerKey>(k => k.Name == "task-1" && k.Group == "tenant-a"), It.IsAny<CancellationToken>()), Times.Once);
        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(new TriggerKey("task-1", "tenant-a"));
        scheduler.Verify(s => s.UnscheduleJob(retryKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnscheduleAsync_RemovesTheStableRetryKeyWithoutGenerationLookup()
    {
        var originalKey = new TriggerKey("task-1", "Default");
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _);

        await sut.UnscheduleAsync("task-1");

        scheduler.Verify(s => s.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(originalKey), It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.GetTriggersOfJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnscheduleAsync_TaskNameEndingWithRetry_DoesNotDeleteAnotherTaskRetryKey()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _);

        await sut.UnscheduleAsync("task-1");

        scheduler.Verify(s => s.UnscheduleJob(new TriggerKey("task-1-retry", "Default"), It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(new TriggerKey("task-1", "Default")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleCronAsync_WhenTriggerScheduledConcurrentlyViaSqlStore_SwallowsWrappedPersistenceException()
    {
        // Arrange: SQL-backed Quartz stores (AdoJobStore) can wrap the duplicate-trigger error in a
        // JobPersistenceException with an inner ObjectAlreadyExistsException. Verify that ScheduleJobAsync
        // handles this wrapping symmetrically with EnsureJobAsync.
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        scheduler.Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobPersistenceException("trigger already exists", new ObjectAlreadyExistsException("duplicate")));

        var sut = CreateScheduler(scheduler, out _);
        var request = CreateNewRequest();

        // Act + Assert: does not throw.
        await sut.ScheduleCronAsync("task-1", request, "0 0/5 * * * ?");
    }

    [Fact]
    public async Task ScheduleAndUnscheduleAsync_UseTheOriginalScheduleCoordinator()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var coordinator = new InlineScheduleCoordinator();
        var sut = CreateScheduler(scheduler, out _, coordinator: coordinator);

        await sut.ScheduleCronAsync("task-1", CreateNewRequest(), "0 0/5 * * * ?");
        await sut.UnscheduleAsync("task-1");

        Assert.Equal(2, coordinator.Keys.Count);
        Assert.All(coordinator.Keys, key => Assert.Equal(new TriggerKey("task-1", "Default"), key));
    }

    private static QuartzWorkflowScheduler CreateScheduler(Mock<global::Quartz.IScheduler> scheduler, out Mock<global::Quartz.ISchedulerFactory> factory, string? tenantId = null, IQuartzScheduleCoordinator? coordinator = null)
    {
        factory = new Mock<global::Quartz.ISchedulerFactory>();
        factory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler.Object);

        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.Tenant).Returns(tenantId == null ? null : new Tenant { Id = tenantId });

        var jobKeyProvider = new JobKeyProvider(tenantAccessor.Object);
        var jsonSerializer = new Mock<IJsonSerializer>();

        return new QuartzWorkflowScheduler(
            factory.Object,
            jsonSerializer.Object,
            tenantAccessor.Object,
            jobKeyProvider,
            NullLogger<QuartzWorkflowScheduler>.Instance,
            coordinator);
    }

    private sealed class InlineScheduleCoordinator : IQuartzScheduleCoordinator
    {
        public List<TriggerKey> Keys { get; } = [];

        public async Task ExecuteAsync(TriggerKey originalTriggerKey, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            Keys.Add(originalTriggerKey);
            await action(cancellationToken);
        }
    }

    private static global::Elsa.Scheduling.ScheduleNewWorkflowInstanceRequest CreateNewRequest() => new()
    {
        WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId("definition-version-1"),
        TriggerActivityId = "activity-1"
    };
}

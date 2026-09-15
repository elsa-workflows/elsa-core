using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Hangfire.Jobs;
using Elsa.Scheduling.Hangfire.Services;
using Elsa.Scheduling;
using Elsa.Workflows.Models;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Moq;

namespace Elsa.Scheduling.Hangfire.UnitTests.Services;

/// <summary>
/// Tests for <see cref="HangfireWorkflowScheduler"/> scheduling and unschedule behavior.
/// </summary>
public class HangfireWorkflowSchedulerTests
{
    [Fact]
    public async Task UnscheduleAsync_RemovesResumeWorkflowJobRecurringJob()
    {
        // Arrange: cron resume for an existing workflow instance.
        var (sut, storage) = CreateScheduler();
        var request = CreateExistingInstanceRequest();

        await sut.ScheduleCronAsync("task-1", request, "* * * * *");
        Assert.Contains(GetRecurringJobs(storage), job => job.Id == "task-1" && job.Job.Type == typeof(ResumeWorkflowJob));

        // Act
        await sut.UnscheduleAsync("task-1");

        // Assert: the resume recurring job is gone.
        Assert.DoesNotContain(GetRecurringJobs(storage), job => job.Id == "task-1");
    }

    [Fact]
    public async Task UnscheduleAsync_RemovesResumeWorkflowJobScheduledViaScheduleRecurringAsync()
    {
        // Arrange: interval resume is a delayed job (not a cron recurring job).
        var (sut, storage) = CreateScheduler();
        var request = CreateExistingInstanceRequest();

        await sut.ScheduleRecurringAsync("task-1", request, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(5));
        Assert.NotEmpty(GetScheduledJobIds(storage, "task-1"));

        // Act
        await sut.UnscheduleAsync("task-1");

        // Assert
        Assert.Empty(GetScheduledJobIds(storage, "task-1"));
    }

    [Fact]
    public async Task UnscheduleAsync_RemovesRunWorkflowJobRecurringJob()
    {
        // Arrange: cron start for a new workflow instance (the path that already worked).
        var (sut, storage) = CreateScheduler();
        var request = CreateNewInstanceRequest();

        await sut.ScheduleCronAsync("task-1", request, "* * * * *");
        Assert.Contains(GetRecurringJobs(storage), job => job.Id == "task-1" && job.Job.Type == typeof(RunWorkflowJob));

        // Act
        await sut.UnscheduleAsync("task-1");

        // Assert
        Assert.DoesNotContain(GetRecurringJobs(storage), job => job.Id == "task-1");
    }

    [Fact]
    public async Task UnscheduleAsync_DoesNotRemoveRecurringJobsForADifferentTaskName()
    {
        // Arrange
        var (sut, storage) = CreateScheduler();
        var request = CreateExistingInstanceRequest();

        await sut.ScheduleCronAsync("task-1", request, "* * * * *");
        await sut.ScheduleCronAsync("task-2", request, "* * * * *");

        // Act
        await sut.UnscheduleAsync("task-1");

        // Assert
        var remaining = GetRecurringJobs(storage).ToList();
        Assert.DoesNotContain(remaining, job => job.Id == "task-1");
        Assert.Contains(remaining, job => job.Id == "task-2" && job.Job.Type == typeof(ResumeWorkflowJob));
    }

    [Fact]
    public async Task UnscheduleAsync_RemovesScheduledResumeWorkflowJob()
    {
        // Arrange: one-shot ScheduleAt for an existing instance.
        var (sut, storage) = CreateScheduler();
        var request = CreateExistingInstanceRequest();

        await sut.ScheduleAtAsync("task-1", request, DateTimeOffset.UtcNow.AddHours(1));
        Assert.NotEmpty(GetScheduledJobIds(storage, "task-1"));

        // Act
        await sut.UnscheduleAsync("task-1");

        // Assert
        Assert.Empty(GetScheduledJobIds(storage, "task-1"));
    }

    [Fact]
    public async Task ScheduleRecurringAsync_NewInstance_HonorsStartAtAndDoesNotCreateCronJob()
    {
        var (sut, storage) = CreateScheduler();
        var startAt = DateTimeOffset.UtcNow.AddHours(3);
        var interval = TimeSpan.FromHours(1);

        await sut.ScheduleRecurringAsync("task-1", CreateNewInstanceRequest(), startAt, interval);

        var scheduled = Assert.Single(GetScheduledJobs(storage, "task-1"));
        Assert.Equal(typeof(RunWorkflowJob), scheduled.Job.Type);
        Assert.Equal(nameof(RunWorkflowJob.ExecuteRecurringAsync), scheduled.Job.Method.Name);
        Assert.Equal(startAt, (DateTimeOffset)scheduled.Job.Args[3]);
        Assert.Equal(interval, (TimeSpan)scheduled.Job.Args[4]);
        Assert.Equal(startAt.UtcDateTime, scheduled.EnqueueAt, TimeSpan.FromSeconds(1));
        Assert.Empty(GetRecurringJobs(storage));
    }

    [Fact]
    public async Task ScheduleRecurringAsync_ExistingInstance_HonorsStartAtAndDoesNotCreateCronJob()
    {
        var (sut, storage) = CreateScheduler();
        var startAt = DateTimeOffset.UtcNow.AddHours(3);
        var interval = TimeSpan.FromMinutes(30);

        await sut.ScheduleRecurringAsync("task-1", CreateExistingInstanceRequest(), startAt, interval);

        var scheduled = Assert.Single(GetScheduledJobs(storage, "task-1"));
        Assert.Equal(typeof(ResumeWorkflowJob), scheduled.Job.Type);
        Assert.Equal(nameof(ResumeWorkflowJob.ExecuteRecurringAsync), scheduled.Job.Method.Name);
        Assert.Equal(startAt, (DateTimeOffset)scheduled.Job.Args[3]);
        Assert.Equal(interval, (TimeSpan)scheduled.Job.Args[4]);
        Assert.Equal(startAt.UtcDateTime, scheduled.EnqueueAt, TimeSpan.FromSeconds(1));
        Assert.Empty(GetRecurringJobs(storage));
    }

    [Theory]
    [MemberData(nameof(CommonIntervals))]
    public async Task ScheduleRecurringAsync_StoresIntervalAsTimeSpanWithoutCronMapping(TimeSpan interval)
    {
        // Quartz/Local keep the TimeSpan. The old Hangfire path mapped e.g. 1h to "* * */1 * * *"
        // (every second of every minute of every hour) via ToCronExpression.
        var (sut, storage) = CreateScheduler();
        var startAt = DateTimeOffset.UtcNow.AddHours(6);

        await sut.ScheduleRecurringAsync("task-1", CreateNewInstanceRequest(), startAt, interval);

        var scheduled = Assert.Single(GetScheduledJobs(storage, "task-1"));
        Assert.Equal(interval, (TimeSpan)scheduled.Job.Args[4]);
        Assert.DoesNotContain(scheduled.Job.Args, arg => arg is string s && s.Contains('*'));
        Assert.Empty(GetRecurringJobs(storage));
    }

    [Fact]
    public async Task ScheduleRecurringAsync_ReplacesLegacyCronRecurringJobForTheSameTask()
    {
        var (sut, storage) = CreateScheduler();
        var request = CreateNewInstanceRequest();

        await sut.ScheduleCronAsync("task-1", request, "* * * * *");
        Assert.Contains(GetRecurringJobs(storage), job => job.Id == "task-1");

        await sut.ScheduleRecurringAsync("task-1", request, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromHours(1));

        Assert.DoesNotContain(GetRecurringJobs(storage), job => job.Id == "task-1");
        Assert.NotEmpty(GetScheduledJobIds(storage, "task-1"));
    }

    public static TheoryData<TimeSpan> CommonIntervals =>
    [
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(24)
    ];

    private static (HangfireWorkflowScheduler Sut, JobStorage Storage) CreateScheduler()
    {
        var storage = new MemoryStorage();
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.Tenant).Returns((Tenant?)null);

        var sut = new HangfireWorkflowScheduler(
            new BackgroundJobClient(storage),
            new RecurringJobManager(storage),
            tenantAccessor.Object,
            storage);

        return (sut, storage);
    }

    private static IReadOnlyList<RecurringJobDto> GetRecurringJobs(JobStorage storage)
    {
        using var connection = storage.GetConnection();
        return connection.GetRecurringJobs();
    }

    private static IReadOnlyList<string> GetScheduledJobIds(JobStorage storage, string taskName)
    {
        return storage.GetMonitoringApi()
            .ScheduledJobs(0, 100)
            .Where(job => job.Value.Job?.Args is { Count: > 0 } && (string)job.Value.Job.Args[0] == taskName)
            .Select(job => job.Key)
            .ToList();
    }

    private static IReadOnlyList<ScheduledJobDto> GetScheduledJobs(JobStorage storage, string taskName)
    {
        return storage.GetMonitoringApi()
            .ScheduledJobs(0, 100)
            .Where(job => job.Value.Job?.Args is { Count: > 0 } && (string)job.Value.Job.Args[0] == taskName)
            .Select(job => job.Value)
            .ToList();
    }

    private static ScheduleExistingWorkflowInstanceRequest CreateExistingInstanceRequest() => new()
    {
        WorkflowInstanceId = "instance-1"
    };

    private static ScheduleNewWorkflowInstanceRequest CreateNewInstanceRequest() => new()
    {
        WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId("definition-version-1"),
        TriggerActivityId = "activity-1"
    };
}

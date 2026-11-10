using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Hangfire.Jobs;
using Elsa.Scheduling.Hangfire.Services;
using Elsa.Workflows.Runtime;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage.Monitoring;
using Moq;

namespace Elsa.Scheduling.Hangfire.UnitTests.Services;

/// <summary>
/// Tests for <see cref="HangfireBackgroundActivityScheduler" /> tenant persistence and legacy job compatibility.
/// </summary>
public class HangfireBackgroundActivitySchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_DefaultTenant_PersistsEmptyTenantId()
    {
        // The scheduler must persist the normalized TenantId ("") so the job runs in the default tenant context.
        var (sut, storage) = CreateScheduler(Tenant.DefaultTenantId);

        await sut.ScheduleAsync(CreateActivity());

        var enqueued = GetEnqueuedJobs(storage, "default");
        var job = Assert.Single(enqueued.Values);
        Assert.Equal(typeof(ExecuteBackgroundActivityJob), job.Job.Type);
        Assert.Equal(Tenant.DefaultTenantId, (string)job.Job.Args[1]!);
    }

    [Fact]
    public async Task ScheduleAsync_NamedTenant_PersistsTenantId()
    {
        var (sut, storage) = CreateScheduler("tenant-a");

        await sut.ScheduleAsync(CreateActivity());

        var enqueued = GetEnqueuedJobs(storage, "default");
        var job = Assert.Single(enqueued.Values);
        Assert.Equal("tenant-a", (string)job.Job.Args[1]!);
    }

    [Fact]
    public async Task ScheduleAsync_UnresolvedTenant_PersistsNormalizedEmptyTenantId()
    {
        // Regression: when no tenant was resolved in context (Tenant == null), the scheduler must persist the
        // accessor's normalized TenantId ("") — not null — so the job still runs in the default tenant context.
        // Reading Tenant?.Id instead of TenantId persists null and this test fails.
        var (sut, storage) = CreateScheduler();

        await sut.ScheduleAsync(CreateActivity());

        var enqueued = GetEnqueuedJobs(storage, "default");
        var job = Assert.Single(enqueued.Values);
        Assert.Equal(typeof(ExecuteBackgroundActivityJob), job.Job.Type);
        Assert.NotNull(job.Job.Args[1]);
        Assert.Equal(Tenant.DefaultTenantId, (string)job.Job.Args[1]!);
    }

    [Fact]
    public async Task CancelAsync_RemovesLegacyJobPersistedWithNullTenantId()
    {
        // Jobs persisted before tenant normalization store a null tenantId; they must still be cancellable by ID.
        var (sut, storage) = CreateScheduler();
        var jobId = new BackgroundJobClient(storage).Enqueue<ExecuteBackgroundActivityJob>(job => job.ExecuteAsync(CreateActivity(), null, CancellationToken.None));
        Assert.Contains(jobId, GetEnqueuedJobs(storage, "default").Keys);

        await sut.CancelAsync(jobId);

        // Hangfire transitions the job to the Deleted state, which removes it from execution.
        using var connection = storage.GetConnection();
        var jobData = connection.GetJobData(jobId);
        Assert.NotNull(jobData);
        Assert.Equal("Deleted", jobData!.State);
    }

    private static (HangfireBackgroundActivityScheduler Sut, MemoryStorage Storage) CreateScheduler(string? tenantId = null)
    {
        var storage = new MemoryStorage();
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.Tenant).Returns(tenantId == null ? null : string.IsNullOrEmpty(tenantId) ? Tenant.Default : new Tenant { Id = tenantId });
        tenantAccessor.Setup(t => t.TenantId).Returns(tenantId ?? Tenant.DefaultTenantId);

        var sut = new HangfireBackgroundActivityScheduler(new BackgroundJobClient(storage), tenantAccessor.Object);

        return (sut, storage);
    }

    private static ScheduledBackgroundActivity CreateActivity() => new("instance-1", "node-1", "bookmark-1");

    private static IReadOnlyDictionary<string, EnqueuedJobDto> GetEnqueuedJobs(MemoryStorage storage, string queue) =>
        storage.GetMonitoringApi().EnqueuedJobs(queue, 0, 100)
            .Where(job => job.Value.Job?.Args is { Count: > 0 } && job.Value.Job.Args[0] is ScheduledBackgroundActivity)
            .ToDictionary(job => job.Key, job => job.Value);
}

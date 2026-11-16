using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Notifications;
using Elsa.Alterations.Core.Results;
using Elsa.Alterations.Services;
using Elsa.Common;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.UnitTests.Services;

public class AlterationFailureStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "A job whose alteration run throws is saved as Failed with the exception message in its log")]
    public async Task RunAsync_WhenAlterationRunnerThrows_FailsJobAndRecordsMessage()
    {
        var jobStore = new FakeJobStore(CreatePendingJob());
        var notifications = new FakeNotificationSender();
        var runner = CreateRunner(jobStore, new ThrowingAlterationRunner(new InvalidOperationException("workflow instance not found")), notifications);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync("job-1"));

        var saved = jobStore.Saves.Last();
        Assert.Equal(AlterationJobStatus.Failed, saved.Status);
        Assert.Equal(Now, saved.CompletedAt);
        var entry = Assert.Single(saved.Log);
        Assert.Equal("workflow instance not found", entry.Message);
        Assert.Equal(LogLevel.Error, entry.LogLevel);
        Assert.Single(notifications.Sent.OfType<AlterationJobCompleted>());
    }

    [Fact(DisplayName = "A job whose alteration run is cancelled is not marked Failed and no completion is sent")]
    public async Task RunAsync_WhenCancelled_PropagatesWithoutFailingJob()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var jobStore = new FakeJobStore(CreatePendingJob());
        var notifications = new FakeNotificationSender();
        var alterationRunner = new ThrowingAlterationRunner(new OperationCanceledException(cancellationTokenSource.Token), cancellationTokenSource);
        var runner = CreateRunner(jobStore, alterationRunner, notifications);

        await Assert.ThrowsAsync<OperationCanceledException>(() => runner.RunAsync("job-1", cancellationTokenSource.Token));

        Assert.DoesNotContain(jobStore.Saves, x => x.Status == AlterationJobStatus.Failed);
        Assert.Equal(AlterationJobStatus.Running, jobStore.Saves.Last().Status);
        Assert.Empty(notifications.Sent.OfType<AlterationJobCompleted>());
    }

    [Fact(DisplayName = "A job whose alteration run throws after cancellation is not marked Failed and no completion is sent")]
    public async Task RunAsync_WhenRunnerThrowsAfterCancellation_PropagatesWithoutFailingJob()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var jobStore = new FakeJobStore(CreatePendingJob());
        var notifications = new FakeNotificationSender();
        var alterationRunner = new ThrowingAlterationRunner(new InvalidOperationException("operation was cancelled by the driver"), cancellationTokenSource);
        var runner = CreateRunner(jobStore, alterationRunner, notifications);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync("job-1", cancellationTokenSource.Token));

        Assert.DoesNotContain(jobStore.Saves, x => x.Status == AlterationJobStatus.Failed);
        Assert.Equal(AlterationJobStatus.Running, jobStore.Saves.Last().Status);
        Assert.Empty(notifications.Sent.OfType<AlterationJobCompleted>());
    }

    [Fact(DisplayName = "CompletePlanAsync stores the plan as Failed when a job failed")]
    public async Task CompletePlanAsync_WhenAJobFailed_MarksPlanFailed()
    {
        var plan = new AlterationPlan { Id = "plan-1", Status = AlterationPlanStatus.Running };
        var jobStore = new FakeJobStore(
            new AlterationJob { Id = "job-1", PlanId = "plan-1", Status = AlterationJobStatus.Completed },
            new AlterationJob { Id = "job-2", PlanId = "plan-1", Status = AlterationJobStatus.Failed });
        var planStore = new FakePlanStore(plan);
        var manager = new AlterationPlanManager(planStore, jobStore, new FakeClock(), new FakeNotificationSender());

        await manager.CompletePlanAsync(plan);

        var saved = planStore.Saves.Single();
        Assert.Equal(AlterationPlanStatus.Failed, saved.Status);
        Assert.Equal(Now, saved.CompletedAt);
    }

    [Fact(DisplayName = "CompletePlanAsync stores the plan as Completed when no job failed")]
    public async Task CompletePlanAsync_WhenNoJobFailed_MarksPlanCompleted()
    {
        var plan = new AlterationPlan { Id = "plan-1", Status = AlterationPlanStatus.Running };
        var jobStore = new FakeJobStore(
            new AlterationJob { Id = "job-1", PlanId = "plan-1", Status = AlterationJobStatus.Completed },
            new AlterationJob { Id = "job-2", PlanId = "plan-2", Status = AlterationJobStatus.Failed });
        var planStore = new FakePlanStore(plan);
        var manager = new AlterationPlanManager(planStore, jobStore, new FakeClock(), new FakeNotificationSender());

        await manager.CompletePlanAsync(plan);

        Assert.Equal(AlterationPlanStatus.Completed, planStore.Saves.Single().Status);
    }

    private static AlterationJob CreatePendingJob() =>
        new() { Id = "job-1", PlanId = "plan-1", WorkflowInstanceId = "instance-1", Status = AlterationJobStatus.Pending };

    private static DefaultAlterationJobRunner CreateRunner(FakeJobStore jobStore, IAlterationRunner alterationRunner, FakeNotificationSender notifications) =>
        new(new FakePlanStore(new AlterationPlan { Id = "plan-1" }), jobStore, alterationRunner, notifications, new FakeClock());

    private class FakeClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private class FakeNotificationSender : INotificationSender
    {
        public List<INotification> Sent { get; } = new();

        public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
        {
            Sent.Add(notification);
            return Task.CompletedTask;
        }

        public Task SendAsync(INotification notification, IEventPublishingStrategy? strategy, CancellationToken cancellationToken = default)
        {
            Sent.Add(notification);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Throws the given exception from <c>RunAsync</c>, optionally cancelling the token source first.
    /// </summary>
    private class ThrowingAlterationRunner(Exception exception, CancellationTokenSource? cancelBeforeThrow = null) : IAlterationRunner
    {
        public Task<ICollection<RunAlterationsResult>> RunAsync(IEnumerable<string> workflowInstanceIds, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RunAlterationsResult> RunAsync(string workflowInstanceId, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default)
        {
            cancelBeforeThrow?.Cancel();
            throw exception;
        }
    }

    /// <summary>
    /// The job as it was at the moment <c>SaveAsync</c> was called.
    /// </summary>
    private record SavedJob(string Id, AlterationJobStatus Status, DateTimeOffset? CompletedAt, IReadOnlyList<AlterationLogEntry> Log);

    /// <summary>
    /// The plan as it was at the moment <c>SaveAsync</c> was called.
    /// </summary>
    private record SavedPlan(string Id, AlterationPlanStatus Status, DateTimeOffset? CompletedAt);

    private class FakeJobStore(params AlterationJob[] jobs) : IAlterationJobStore
    {
        private readonly List<AlterationJob> _jobs = jobs.ToList();

        public List<SavedJob> Saves { get; } = new();

        public Task SaveAsync(AlterationJob job, CancellationToken cancellationToken = default)
        {
            Saves.Add(new SavedJob(job.Id, job.Status, job.CompletedAt, (job.Log ?? []).ToList()));

            if (!_jobs.Contains(job))
            {
                _jobs.Add(job);
            }

            return Task.CompletedTask;
        }

        public Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AlterationJob?> FindAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(filter.Apply(_jobs.AsQueryable()).FirstOrDefault());
        public Task<IEnumerable<AlterationJob>> FindManyAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<AlterationJob>>(filter.Apply(_jobs.AsQueryable()).ToList());
        public Task<IEnumerable<string>> FindManyIdsAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<string>>(filter.Apply(_jobs.AsQueryable()).Select(x => x.Id).ToList());
        public Task<long> CountAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult((long)filter.Apply(_jobs.AsQueryable()).Count());
    }

    private class FakePlanStore(params AlterationPlan[] plans) : IAlterationPlanStore
    {
        private readonly List<AlterationPlan> _plans = plans.ToList();

        public List<SavedPlan> Saves { get; } = new();

        public Task SaveAsync(AlterationPlan plan, CancellationToken cancellationToken = default)
        {
            Saves.Add(new SavedPlan(plan.Id, plan.Status, plan.CompletedAt));

            if (!_plans.Contains(plan))
            {
                _plans.Add(plan);
            }

            return Task.CompletedTask;
        }

        public Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(filter.Apply(_plans.AsQueryable()).FirstOrDefault());
        public Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default) => Task.FromResult((long)filter.Apply(_plans.AsQueryable()).Count());
    }
}

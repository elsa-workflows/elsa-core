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

    [Fact(DisplayName = "A job whose alteration run throws is Failed with the exception message in its log")]
    public async Task RunAsync_WhenAlterationRunnerThrows_FailsJobAndRecordsMessage()
    {
        var jobStore = new FakeJobStore(new AlterationJob { Id = "job-1", PlanId = "plan-1", WorkflowInstanceId = "instance-1", Status = AlterationJobStatus.Pending });
        var planStore = new FakePlanStore(new AlterationPlan { Id = "plan-1" });
        var notifications = new FakeNotificationSender();
        var runner = new DefaultAlterationJobRunner(planStore, jobStore, new ThrowingAlterationRunner("workflow instance not found"), notifications, new FakeClock());

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync("job-1"));

        var job = jobStore.Jobs.Single();
        Assert.Equal(AlterationJobStatus.Failed, job.Status);
        Assert.Equal(Now, job.CompletedAt);
        var entry = Assert.Single(job.Log!);
        Assert.Equal("workflow instance not found", entry.Message);
        Assert.Equal(LogLevel.Error, entry.LogLevel);
        Assert.Single(notifications.Sent.OfType<AlterationJobCompleted>());
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

        Assert.Equal(AlterationPlanStatus.Failed, planStore.Plans.Single().Status);
        Assert.Equal(Now, plan.CompletedAt);
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

        Assert.Equal(AlterationPlanStatus.Completed, planStore.Plans.Single().Status);
    }

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

    private class ThrowingAlterationRunner(string message) : IAlterationRunner
    {
        public Task<ICollection<RunAlterationsResult>> RunAsync(IEnumerable<string> workflowInstanceIds, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(message);

        public Task<RunAlterationsResult> RunAsync(string workflowInstanceId, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(message);
    }

    private class FakeJobStore(params AlterationJob[] jobs) : IAlterationJobStore
    {
        public List<AlterationJob> Jobs { get; } = jobs.ToList();

        public Task SaveAsync(AlterationJob job, CancellationToken cancellationToken = default)
        {
            if (!Jobs.Contains(job))
                Jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AlterationJob?> FindAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(filter.Apply(Jobs.AsQueryable()).FirstOrDefault());
        public Task<IEnumerable<AlterationJob>> FindManyAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<AlterationJob>>(filter.Apply(Jobs.AsQueryable()).ToList());
        public Task<IEnumerable<string>> FindManyIdsAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<string>>(filter.Apply(Jobs.AsQueryable()).Select(x => x.Id).ToList());
        public Task<long> CountAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default) => Task.FromResult((long)filter.Apply(Jobs.AsQueryable()).Count());
    }

    private class FakePlanStore(params AlterationPlan[] plans) : IAlterationPlanStore
    {
        public List<AlterationPlan> Plans { get; } = plans.ToList();

        public Task SaveAsync(AlterationPlan plan, CancellationToken cancellationToken = default)
        {
            if (!Plans.Contains(plan))
                Plans.Add(plan);
            return Task.CompletedTask;
        }

        public Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(filter.Apply(Plans.AsQueryable()).FirstOrDefault());
        public Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default) => Task.FromResult((long)filter.Apply(Plans.AsQueryable()).Count());
    }
}

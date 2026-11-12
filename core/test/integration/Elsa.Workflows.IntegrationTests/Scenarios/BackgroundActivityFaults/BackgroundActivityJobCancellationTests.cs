using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BackgroundActivityFaults;

/// <summary>
/// A cancellation exception from a background activity only counts as a cancelled job when the job's own token was
/// cancelled. Then the job stops and the workflow is not resumed. Any other cancellation exception, such as an
/// HttpClient timeout, is an activity fault that is carried back to the workflow.
/// </summary>
public class BackgroundActivityJobCancellationTests : IAsyncLifetime
{
    private const string CancellingActivityId = "Cancelling";

    private readonly JobControl _jobControl = new();
    private readonly CapturingBackgroundActivityScheduler _scheduler = new();
    private readonly RecordingBookmarkQueue _bookmarkQueue = new();
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddElsa(elsa => elsa
                    .AddActivity<CancellingActivity>()
                    .UseWorkflowRuntime(runtime => runtime.BackgroundActivityScheduler = _ => _scheduler));
                services.AddSingleton(_jobControl);
                services.AddScoped<IBookmarkQueue>(_ => _bookmarkQueue);
            })
            .Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        _jobControl.Source.Dispose();
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _host.StopAsync(stopTimeout.Token);
        _host.Dispose();
    }

    [Fact(DisplayName = "A cancelled background job stops without resuming the workflow")]
    public async Task CancelledJobDoesNotResumeTheWorkflow()
    {
        _jobControl.CancelJob = true;
        var (instanceId, job) = await CreateSuspendedInstanceAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ExecuteAsync(job));

        Assert.Empty(_bookmarkQueue.Items);
        await AssertWorkflowUnchangedAsync(instanceId);
    }

    [Fact(DisplayName = "A cancellation exception while the background job is not cancelled resumes the workflow with the fault")]
    public async Task CancellationExceptionWithoutJobCancellationResumesWithTheFault()
    {
        _jobControl.CancelJob = false;
        var (instanceId, job) = await CreateSuspendedInstanceAsync();

        await ExecuteAsync(job);

        var item = Assert.Single(_bookmarkQueue.Items);
        Assert.Equal(instanceId, item.WorkflowInstanceId);
        Assert.Equal(job.BookmarkId, item.BookmarkId);
        Assert.NotNull(item.Options?.Properties);
        Assert.Contains($"__BackgroundActivityFault:{job.ActivityNodeId}", item.Options.Properties.Keys);
        await AssertWorkflowUnchangedAsync(instanceId);
    }

    private async Task ExecuteAsync(ScheduledBackgroundActivity job)
    {
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IBackgroundActivityInvoker>().ExecuteAsync(job, _jobControl.Source.Token);
    }

    /// <summary>
    /// Runs a workflow until it suspends at the background activity and stores it. Returns the instance ID and the job
    /// that was scheduled for the background activity, which the test runs itself.
    /// </summary>
    private async Task<(string InstanceId, ScheduledBackgroundActivity Job)> CreateSuspendedInstanceAsync()
    {
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        await services.PopulateRegistriesAsync();
        var workflow = await services.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder()
            .BuildWorkflowAsync(new TestWorkflow(builder => builder.Root = new Sequence
            {
                Activities =
                {
                    new CancellingActivity { Id = CancellingActivityId, RunAsynchronously = true },
                    new WriteLine("Done")
                }
            }));
        await services.GetRequiredService<IWorkflowRegistry>().RegisterAsync(workflow);

        var result = await services.GetRequiredService<IWorkflowRunner>().RunAsync(workflow);
        var state = result.WorkflowState;
        Assert.Equal(WorkflowSubStatus.Suspended, state.SubStatus);
        await services.GetRequiredService<IWorkflowInstanceManager>().SaveAsync(state, CancellationToken.None);

        var job = Assert.Single(_scheduler.Jobs);
        Assert.Equal(state.Id, job.WorkflowInstanceId);
        return (state.Id, job);
    }

    private async Task AssertWorkflowUnchangedAsync(string instanceId)
    {
        using var scope = _host.Services.CreateScope();
        var instance = await scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(instanceId);

        Assert.NotNull(instance);
        Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
        Assert.Empty(instance.WorkflowState.Incidents);
        Assert.Contains(instance.WorkflowState.ActivityExecutionContexts, x => x.ScheduledActivityNodeId.EndsWith(CancellingActivityId) && x.Status == ActivityStatus.Running);

        // Bookmarks are not checked: the memory store hands out the stored WorkflowState itself, and the job's own copy of
        // the workflow removes its bookmarks when the job is cancelled. A database store is not affected.
    }

    /// <summary>Throws a cancellation exception in the background, cancelling the job first when the test asks for it.</summary>
    [Activity("Elsa.Test", Kind = ActivityKind.Task)]
    private class CancellingActivity : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
            var jobControl = context.GetRequiredService<JobControl>();

            if (jobControl.CancelJob)
            {
                jobControl.Source.Cancel();
                throw new OperationCanceledException(jobControl.Source.Token);
            }

            throw new TaskCanceledException("The request timed out.");
        }
    }

    private class JobControl
    {
        public CancellationTokenSource Source { get; } = new();
        public bool CancelJob { get; set; }
    }

    /// <summary>Keeps the scheduled jobs so the test can run them itself, with its own cancellation token.</summary>
    private class CapturingBackgroundActivityScheduler : IBackgroundActivityScheduler
    {
        public List<ScheduledBackgroundActivity> Jobs { get; } = [];

        public Task<string> CreateAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
        {
            Jobs.Add(scheduledBackgroundActivity);
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public Task ScheduleAsync(string jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default) => CreateAsync(scheduledBackgroundActivity, cancellationToken);

        public Task UnscheduledAsync(string jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CancelAsync(string jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>Records resume requests instead of resuming, so the test can check whether the job asked for a resume.</summary>
    private class RecordingBookmarkQueue : IBookmarkQueue
    {
        public List<NewBookmarkQueueItem> Items { get; } = [];

        public Task EnqueueAsync(NewBookmarkQueueItem item, CancellationToken cancellationToken = default)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }
    }
}

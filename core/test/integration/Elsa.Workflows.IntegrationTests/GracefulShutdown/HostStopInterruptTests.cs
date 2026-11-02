using System.Collections.Concurrent;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.HostedServices;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// A host-stop drain whose deadline passes while a workflow is mid-activity. The cut-off runner is released after the
/// drain, so its late commits are observed, and the same host then runs the recovery that the next boot would run.
/// </summary>
public class HostStopInterruptTests(ITestOutputHelper testOutputHelper) : IAsyncDisposable
{
    private readonly HostStopGate _gate = HostStopGate.Current = new();
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-host-stop-{Guid.NewGuid():N}.db");
    private IServiceProvider _services = null!;
    private BackgroundCommandSenderHostedService? _commandSender;

    [Theory(DisplayName = "A cycle that committed a checkpoint ends Cancelled when force-cancelled, and Interrupted and recovered at that checkpoint when interrupted")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckpointedCycle(bool interruptOnHostStop)
    {
        await StartAsync(interruptOnHostStop, new ActivityExecutingWorkflowStrategy());
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        Assert.False((await client.CreateInstanceAsync(new() { WorkflowDefinitionHandle = Handle<HostStopCheckpointWorkflow>() })).CannotStart);
        var run = Task.Run(() => client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty));

        var instance = await DrainAndReleaseAsync(client.WorkflowInstanceId, run);

        if (!interruptOnHostStop)
        {
            // The cut-off runner's late commit overwrites the Interrupted mark, so nothing is recovered.
            Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Cancelled), (instance.Status, instance.SubStatus));
            Assert.Equal(0, await ScanAsync());
            return;
        }

        // The cut-off runner finished the workflow, but its commits were dropped, so the checkpoint is still there.
        Assert.Equal((WorkflowStatus.Running, WorkflowSubStatus.Interrupted, false), (instance.Status, instance.SubStatus, instance.IsExecuting));
        Assert.Contains(instance.WorkflowState.ActivityExecutionContexts, x => x.IsExecuting);
        Assert.Equal(1, await ScanAsync());
        await WaitForFinishedAsync(client.WorkflowInstanceId);
        Assert.Equal(1, _gate.Count("first"));
        Assert.Equal(2, _gate.Count(HostStopHang.HangName));
    }

    [Theory(DisplayName = "A resume that never reached a checkpoint ends Cancelled when force-cancelled, and keeps its suspension and is resumed again when interrupted")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UncommittedResume(bool interruptOnHostStop)
    {
        await StartAsync(interruptOnHostStop, commitStrategy: null);
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var suspended = await client.CreateAndRunInstanceAsync(new() { WorkflowDefinitionHandle = Handle<HostStopResumeWorkflow>() });
        Assert.Equal(WorkflowSubStatus.Suspended, suspended.SubStatus);
        var bookmarkId = Assert.Single(suspended.Bookmarks!).Id;
        var run = Task.Run(() => client.RunInstanceAsync(new() { BookmarkId = bookmarkId }));

        var instance = await DrainAndReleaseAsync(client.WorkflowInstanceId, run);

        if (!interruptOnHostStop)
        {
            Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Cancelled), (instance.Status, instance.SubStatus));
            Assert.Equal(0, await ScanAsync());
            return;
        }

        // The suspension is not marked Interrupted, which would make the scan run the workflow again from its root.
        // Instead, the resume is queued again; the bookmark queue worker may pick it up before this test processes it.
        Assert.NotEqual(WorkflowSubStatus.Interrupted, instance.SubStatus);
        Assert.Equal(0, await ScanAsync());
        await WaitForFinishedAsync(client.WorkflowInstanceId, processBookmarkQueue: true);
        Assert.Equal(1, _gate.Count("before"));
    }

    [Theory(DisplayName = "A fresh run cut off before its first commit ends Cancelled when force-cancelled, and is persisted as not started and started again when interrupted")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UncommittedFreshRun(bool interruptOnHostStop)
    {
        await StartAsync(interruptOnHostStop, commitStrategy: null);
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var run = Task.Run(() => client.CreateAndRunInstanceAsync(new() { WorkflowDefinitionHandle = Handle<HostStopCheckpointWorkflow>() }));
        await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Null(await FindAsync(client.WorkflowInstanceId));

        var instance = await DrainAndReleaseAsync(client.WorkflowInstanceId, run);

        if (!interruptOnHostStop)
        {
            Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Cancelled), (instance.Status, instance.SubStatus));
            Assert.Equal(0, await ScanAsync());
            return;
        }

        Assert.Equal((WorkflowStatus.Running, WorkflowSubStatus.Interrupted, WorkflowSubStatus.Pending), (instance.Status, instance.SubStatus, instance.WorkflowState.SubStatus));
        Assert.Equal(1, await ScanAsync());
        await WaitForFinishedAsync(client.WorkflowInstanceId);
        Assert.Equal(2, _gate.Count(HostStopHang.HangName));
    }

    public async ValueTask DisposeAsync()
    {
        _gate.Continue.TrySetResult();

        if (_commandSender is not null)
            await _commandSender.StopAsync(CancellationToken.None);

        if (_services is IAsyncDisposable disposable)
            await disposable.DisposeAsync();

        File.Delete(_databasePath);
    }

    // SQLite rather than the memory stores, which hand out the stored state object itself, so a cut-off runner would
    // change it without committing; after a real host stop, only what was committed is left.
    private async Task StartAsync(bool interruptOnHostStop, IWorkflowCommitStrategy? commitStrategy)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddActivitiesFrom<HostStopInterruptTests>()
            .AddWorkflow<HostStopCheckpointWorkflow>()
            .AddWorkflow<HostStopResumeWorkflow>()
            .ConfigureServices(services => services.Configure<CommitStateOptions>(options => options.DefaultWorkflowCommitStrategy = commitStrategy))
            .ConfigureElsa(elsa => elsa
                .UseWorkflowManagement(management => management.UseEntityFrameworkCore(persistence => persistence.UseSqlite(ConnectionString)))
                .UseWorkflowRuntime(runtime =>
                {
                    runtime.UseEntityFrameworkCore(persistence => persistence.UseSqlite(ConnectionString));
                    runtime.ConfigureGracefulShutdown(options =>
                    {
                        options.DrainDeadline = TimeSpan.FromMilliseconds(50);
                        options.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
                        options.InterruptOnHostStop = interruptOnHostStop;
                    });
                }))
            .Build();

        await using (var management = await _services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            await management.Database.MigrateAsync();
        await using (var runtime = await _services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync())
            await runtime.Database.MigrateAsync();

        await _services.PopulateRegistriesAsync();
    }

    /// <summary>Drains on host stop while the workflow hangs, then lets the cut-off runner finish and returns the row it left.</summary>
    private async Task<WorkflowInstance> DrainAndReleaseAsync(string instanceId, Task run)
    {
        await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var outcome = await _services.GetRequiredService<IDrainOrchestrator>().DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.DeadlineExceeded, outcome.OverallResult);
        Assert.Contains(instanceId, outcome.ForceCancelledInstanceIds);

        _gate.Continue.TrySetResult();
        try { await run.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) when (!ex.IsFatal()) { /* the cut-off runner may surface an exception */ }

        var instance = await FindAsync(instanceId);
        Assert.NotNull(instance);
        return instance;
    }

    private async Task<int> ScanAsync()
    {
        if (_commandSender is null)
        {
            _commandSender = _services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
            await _commandSender.StartAsync(CancellationToken.None);
        }

        using var scope = _services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IInterruptedRecoveryScanner>().ScanAndRequeueAsync(CancellationToken.None);
    }

    private async Task<WorkflowInstance?> FindAsync(string instanceId)
    {
        using var scope = _services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = instanceId });
    }

    private async Task WaitForFinishedAsync(string instanceId, bool processBookmarkQueue = false)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

        while (await FindAsync(instanceId) is var instance && instance is not { Status: WorkflowStatus.Finished, SubStatus: WorkflowSubStatus.Finished })
        {
            Assert.True(DateTime.UtcNow < deadline, $"Instance {instanceId} was not recovered; it is {instance?.Status}/{instance?.SubStatus}.");

            if (processBookmarkQueue)
            {
                using var scope = _services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>().ProcessAsync();
            }

            await Task.Delay(20);
        }
    }

    private string ConnectionString => $"Data Source={_databasePath};Default Timeout=30;Pooling=False";

    private static WorkflowDefinitionHandle Handle<TWorkflow>() => WorkflowDefinitionHandle.ByDefinitionId(typeof(TWorkflow).Name, VersionOptions.Published);
}

/// <summary>Counts executions and holds the first <see cref="HostStopHang"/> as if its host were stopping.</summary>
public sealed class HostStopGate
{
    private readonly ConcurrentDictionary<string, int> _counts = new();

    public static HostStopGate Current { get; set; } = new();

    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Increment(string name) => _counts.AddOrUpdate(name, 1, (_, count) => count + 1);
    public int Count(string name) => _counts.GetValueOrDefault(name);
}

/// <summary>Counts its executions under its name.</summary>
public class HostStopStep : CodeActivity
{
    public string StepName { get; set; } = null!;

    protected override void Execute(ActivityExecutionContext context) => HostStopGate.Current.Increment(StepName);
}

/// <summary>Hangs on its first execution, ignoring cancellation, and completes on any later one.</summary>
public class HostStopHang : CodeActivity
{
    public const string HangName = "hang";

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (HostStopGate.Current.Increment(HangName) != 1)
            return;

        HostStopGate.Current.Started.TrySetResult();
        await HostStopGate.Current.Continue.Task;
    }
}

public class HostStopCheckpointWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder) =>
        builder.Root = new Sequence { Activities = { new HostStopStep { StepName = "first" }, new HostStopHang(), new HostStopStep { StepName = "second" } } };
}

public class HostStopResumeWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder) =>
        builder.Root = new Sequence { Activities = { new HostStopStep { StepName = "before" }, new Event("host-stop-go"), new HostStopHang(), new HostStopStep { StepName = "after" } } };
}

using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// A drain's force-cancel reaches a running activity through <see cref="ActivityExecutionContext.CancellationToken"/> (#8489).
/// </summary>
public class CooperativeDrainCancellationTests(ITestOutputHelper testOutputHelper) : IAsyncDisposable
{
    private readonly CooperativeWaitGate _gate = new();
    private readonly TokenIgnoringWaitGate _ignoringGate = new();
    private IServiceProvider _services = null!;
    private BackgroundCommandSenderHostedService? _commandSender;

    [Fact(DisplayName = "A drain force-cancel stops an activity that honours its token, leaves the instance Running/Interrupted, and recovery re-runs the activity once")]
    public async Task DrainStopsTokenHonouringActivityAndRecoveryReRunsIt()
    {
        // Arrange
        await StartAsync();
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        Assert.False((await client.CreateInstanceAsync(new() { WorkflowDefinitionHandle = Handle<CooperativeWaitWorkflow>() })).CannotStart);
        var run = Task.Run(() => client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty));
        await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Act
        var outcome = await _services.GetRequiredService<IDrainOrchestrator>().DrainAsync(DrainTrigger.HostStopSignal);

        // Assert: the activity stopped while the drain waited for the cycle to settle, so the runner committed nothing after its checkpoint.
        Assert.True(_gate.Stopped.Task.IsCompleted);
        Assert.Equal(DrainResult.DeadlineExceeded, outcome.OverallResult);
        Assert.Contains(client.WorkflowInstanceId, outcome.ForceCancelledInstanceIds);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
        var interrupted = await FindAsync(client.WorkflowInstanceId);
        Assert.Equal((WorkflowStatus.Running, WorkflowSubStatus.Interrupted), (interrupted!.Status, interrupted.SubStatus));

        Assert.Equal(1, await ScanAsync());
        await WaitForStatusAsync(client.WorkflowInstanceId, WorkflowStatus.Finished, WorkflowSubStatus.Finished);
        Assert.Equal(2, _gate.Executions);
    }

    [Fact(DisplayName = "A drain force-cancel cannot stop an activity that ignores its token: the instance ends Finished/Cancelled and is not recovered")]
    public async Task DrainOfTokenIgnoringActivityEndsCancelledWithoutRecovery()
    {
        // Arrange
        await StartAsync();
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        Assert.False((await client.CreateInstanceAsync(new() { WorkflowDefinitionHandle = Handle<TokenIgnoringWaitWorkflow>() })).CannotStart);
        var run = Task.Run(() => client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty));
        await _ignoringGate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Act
        var outcome = await _services.GetRequiredService<IDrainOrchestrator>().DrainAsync(DrainTrigger.HostStopSignal);
        _ignoringGate.Continue.TrySetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert: best effort only. The activity finished after the cancel, so the runner committed Cancelled.
        Assert.Contains(client.WorkflowInstanceId, outcome.ForceCancelledInstanceIds);
        var cancelled = await FindAsync(client.WorkflowInstanceId);
        Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Cancelled), (cancelled!.Status, cancelled.SubStatus));
        Assert.Equal(0, await ScanAsync());
        Assert.Equal(1, _ignoringGate.Executions);
    }

    [Fact(DisplayName = "On host stop, the drain stops before the background command sender, so a drained background run ends Running/Interrupted")]
    public async Task HostStopDrainsBeforeTheCommandSenderStops()
    {
        // Arrange: a real host, with the workflow dispatched to the background command sender, whose runs observe its stopping token.
        using var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.AddProvider(new XunitLoggerProvider(testOutputHelper)))
            .ConfigureServices(services => services
                .AddSingleton(_gate)
                .AddElsa(elsa => elsa
                    .AddActivitiesFrom<CooperativeWait>()
                    .AddWorkflow<CooperativeWaitWorkflow>()
                    .UseWorkflowManagement()
                    .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(options =>
                    {
                        options.DrainDeadline = TimeSpan.FromMilliseconds(50);
                        options.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
                    }))))
            .Build();
        await host.StartAsync();
        await host.Services.PopulateRegistriesAsync();
        var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        Assert.False((await client.CreateInstanceAsync(new() { WorkflowDefinitionHandle = Handle<CooperativeWaitWorkflow>() })).CannotStart);
        (await host.Services.GetRequiredService<IWorkflowDispatcher>().DispatchAsync(new DispatchWorkflowInstanceRequest(client.WorkflowInstanceId), options: null)).ThrowIfFailed();
        await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Act
        await host.StopAsync();

        // Assert: if the command sender stopped first, its stopping token would cancel the run as a caller-token cancel,
        // and the instance would end Finished/Cancelled with an incident instead.
        Assert.True(_gate.Stopped.Task.IsCompleted);
        using var scope = host.Services.CreateScope();
        var instance = await scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = client.WorkflowInstanceId });
        Assert.Equal((WorkflowStatus.Running, WorkflowSubStatus.Interrupted), (instance!.Status, instance.SubStatus));
        Assert.Empty(instance.WorkflowState.Incidents);
    }

    [Fact(DisplayName = "Cancelling the caller's token during an activity that honours it still persists Finished/Cancelled with an incident")]
    public async Task CallerTokenCancelPersistsFinishedCancelledWithIncident()
    {
        // Arrange
        await StartAsync();
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        Assert.False((await client.CreateInstanceAsync(new() { WorkflowDefinitionHandle = Handle<CooperativeWaitWorkflow>() })).CannotStart);
        using var callerCancellation = new CancellationTokenSource();
        var run = Task.Run(() => client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty, callerCancellation.Token));
        await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Act
        await callerCancellation.CancelAsync();
        await _gate.Stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        var cancelled = await FindAsync(client.WorkflowInstanceId);
        Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Cancelled), (cancelled!.Status, cancelled.SubStatus));
        Assert.NotEmpty(cancelled.WorkflowState.Incidents);
    }

    [Fact(DisplayName = "A user cancel still persists Finished/Cancelled")]
    public async Task UserCancelPersistsFinishedCancelled()
    {
        // Arrange
        await StartAsync();
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var suspended = await client.CreateAndRunInstanceAsync(new() { WorkflowDefinitionHandle = Handle<SuspendedWorkflow>() });
        Assert.Equal(WorkflowSubStatus.Suspended, suspended.SubStatus);

        // Act
        await client.CancelAsync();

        // Assert
        var cancelled = await FindAsync(client.WorkflowInstanceId);
        Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Cancelled), (cancelled!.Status, cancelled.SubStatus));
    }

    public async ValueTask DisposeAsync()
    {
        if (_commandSender is not null)
            await _commandSender.StopAsync(CancellationToken.None);
    }

    private async Task StartAsync()
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddActivitiesFrom<CooperativeWait>()
            .AddWorkflow<CooperativeWaitWorkflow>()
            .AddWorkflow<TokenIgnoringWaitWorkflow>()
            .AddWorkflow<SuspendedWorkflow>()
            .ConfigureServices(services => services.AddSingleton(_gate).AddSingleton(_ignoringGate))
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(options =>
            {
                options.DrainDeadline = TimeSpan.FromMilliseconds(50);
                options.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
            })))
            .Build();
        await _services.PopulateRegistriesAsync();
    }

    private async Task<int> ScanAsync()
    {
        _commandSender = _services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        await _commandSender.StartAsync(CancellationToken.None);
        using var scope = _services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IInterruptedRecoveryScanner>().ScanAndRequeueAsync(CancellationToken.None);
    }

    private async Task<WorkflowInstance?> FindAsync(string instanceId)
    {
        using var scope = _services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = instanceId });
    }

    private async Task WaitForStatusAsync(string instanceId, WorkflowStatus status, WorkflowSubStatus subStatus)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

        while (await FindAsync(instanceId) is var instance && (instance?.Status, instance?.SubStatus) != (status, subStatus))
        {
            Assert.True(DateTime.UtcNow < deadline, $"Instance {instanceId} is {instance?.Status}/{instance?.SubStatus}, not {status}/{subStatus}.");
            await Task.Delay(20);
        }
    }

    private static WorkflowDefinitionHandle Handle<TWorkflow>() => WorkflowDefinitionHandle.ByDefinitionId(typeof(TWorkflow).Name, VersionOptions.Published);

    public class SuspendedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder) => builder.Root = new Event("cooperative-cancel-go");
    }
}

/// <summary>Counts the executions of <see cref="CooperativeWait"/> and reports when its first execution starts and stops.</summary>
public sealed class CooperativeWaitGate
{
    public int Executions;
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// On its first execution, commits a checkpoint (so the instance is saved with this activity executing) and then waits
/// until its cancellation token is cancelled. Any later execution completes at once.
/// </summary>
public class CooperativeWait : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var gate = context.GetRequiredService<CooperativeWaitGate>();
        if (Interlocked.Increment(ref gate.Executions) > 1)
            return;

        await context.WorkflowExecutionContext.CommitAsync();
        gate.Started.TrySetResult();
        try
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
        finally
        {
            gate.Stopped.TrySetResult();
        }
    }
}

public class CooperativeWaitWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder) => builder.Root = new CooperativeWait();
}

/// <summary>Counts the executions of <see cref="TokenIgnoringWait"/> and lets the test release its first execution.</summary>
public sealed class TokenIgnoringWaitGate
{
    public int Executions;
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Like <see cref="CooperativeWait"/>, but ignores its cancellation token: it waits until the test releases it.
/// </summary>
public class TokenIgnoringWait : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var gate = context.GetRequiredService<TokenIgnoringWaitGate>();
        if (Interlocked.Increment(ref gate.Executions) > 1)
            return;

        await context.WorkflowExecutionContext.CommitAsync();
        gate.Started.TrySetResult();
        await gate.Continue.Task;
    }
}

public class TokenIgnoringWaitWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder) => builder.Root = new TokenIgnoringWait();
}

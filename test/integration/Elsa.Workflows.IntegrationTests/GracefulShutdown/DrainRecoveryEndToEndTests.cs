using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// End-to-end drain recovery using the production <see cref="IWorkflowRestarter"/> (not a recording fake).
/// Asserts the actual post-recovery outcome: a genuinely interrupted Running instance resumes or completes,
/// and a drain-cancelled Finished/Cancelled instance is not requeued.
/// </summary>
public class DrainRecoveryEndToEndTests
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IDrainOrchestrator _orchestrator;

    public DrainRecoveryEndToEndTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddActivitiesFrom<DrainRecoveryEndToEndTests>()
            .AddWorkflow<RecoverableWriteLineWorkflow>()
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(o =>
                {
                    o.DrainDeadline = TimeSpan.FromMilliseconds(50);
                    o.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
                })))
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
        _orchestrator = _services.GetRequiredService<IDrainOrchestrator>();
    }

    [Fact(DisplayName = "Real restarter resumes a Running/Interrupted instance and does not resume a drain-cancelled Finished/Cancelled instance")]
    public async Task RealRestarterResumesInterruptedRunningAndLeavesDrainCancelledAlone()
    {
        await _services.PopulateRegistriesAsync();

        var recoverableClient = await _workflowRuntime.CreateClientAsync();
        var created = await recoverableClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(RecoverableWriteLineWorkflow), VersionOptions.Published)
        });
        Assert.False(created.CannotStart);

        using var arrangeScope = _services.CreateScope();
        var instanceStore = arrangeScope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var marked = await instanceStore.TryMarkInterruptedAsync(recoverableClient.WorkflowInstanceId);
        Assert.True(marked, "A never-started Running instance must be markable as Interrupted.");

        var activityState = new ObservableActivityState();
        var drainWorkflow = new TestWorkflow(builder => builder.Root = new ObservableActivity
        {
            State = activityState,
            DelayMs = 500,
        });
        var drainTask = Task.Run(() => _workflowRunner.RunAsync(drainWorkflow));
        await activityState.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var outcome = await _orchestrator.DrainAsync(DrainTrigger.HostStopSignal);

        try { await drainTask.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) when (!ex.IsFatal()) { /* runner may complete normally or surface OCE */ }

        Assert.Equal(DrainResult.DeadlineExceeded, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        var cancelledId = Assert.Single(outcome.ForceCancelledInstanceIds);

        var cancelled = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = cancelledId });
        Assert.NotNull(cancelled);
        Assert.Equal(WorkflowStatus.Finished, cancelled.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, cancelled.SubStatus);

        var commandProcessor = _services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        await commandProcessor.StartAsync(CancellationToken.None);

        try
        {
            using var recoverScope = _services.CreateScope();
            var scanner = recoverScope.ServiceProvider.GetRequiredService<IInterruptedRecoveryScanner>();
            var requeued = await scanner.ScanAndRequeueAsync(CancellationToken.None);

            Assert.Equal(1, requeued);

            await WaitUntilAsync(async () =>
            {
                var current = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = recoverableClient.WorkflowInstanceId });
                return current is { Status: WorkflowStatus.Finished, SubStatus: WorkflowSubStatus.Finished };
            }, TimeSpan.FromSeconds(10));

            var recovered = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = recoverableClient.WorkflowInstanceId });
            Assert.NotNull(recovered);
            Assert.Equal(WorkflowStatus.Finished, recovered.Status);
            Assert.Equal(WorkflowSubStatus.Finished, recovered.SubStatus);

            var stillCancelled = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = cancelledId });
            Assert.NotNull(stillCancelled);
            Assert.Equal(WorkflowStatus.Finished, stillCancelled.Status);
            Assert.Equal(WorkflowSubStatus.Cancelled, stillCancelled.SubStatus);
        }
        finally
        {
            await commandProcessor.StopAsync(CancellationToken.None);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;
            await Task.Delay(20);
        }

        throw new TimeoutException($"Condition was not met within {timeout}.");
    }
}

/// <summary>Published definition that can complete when the real restarter dispatches it.</summary>
public class RecoverableWriteLineWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new WriteLine("recovered");
    }
}

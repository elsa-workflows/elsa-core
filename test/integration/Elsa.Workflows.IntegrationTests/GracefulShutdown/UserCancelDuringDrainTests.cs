using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// A user cancellation that still has its original execution cycle live when drain snapshots
/// must stay <see cref="WorkflowSubStatus.Cancelled"/> and must not be requeued.
/// </summary>
public class UserCancelDuringDrainTests
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IDrainOrchestrator _orchestrator;

    public UserCancelDuringDrainTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddActivitiesFrom<UserCancelDuringDrainTests>()
            .AddWorkflow<LongRunningObservableWorkflow>()
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(o =>
                {
                    o.DrainDeadline = TimeSpan.FromMilliseconds(50);
                    o.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
                })))
            .Build();
        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
        _orchestrator = _services.GetRequiredService<IDrainOrchestrator>();
    }

    [Fact(DisplayName = "Normal cancellation with a still-active execution cycle is not promoted to Interrupted or requeued after drain")]
    public async Task NormalCancelThenDrainDoesNotRequeue()
    {
        await _services.PopulateRegistriesAsync();

        var client = await _workflowRuntime.CreateClientAsync();
        await client.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(LongRunningObservableWorkflow), VersionOptions.Published)
        });

        var cycles = _services.GetRequiredService<IExecutionCycleRegistry>();
        var runTask = Task.Run(() => client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty));
        await WaitUntilAsync(() => cycles.ActiveCount > 0, TimeSpan.FromSeconds(2));

        await client.CancelAsync();

        using var scope = _services.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        await WaitUntilAsync(async () =>
        {
            var current = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = client.WorkflowInstanceId });
            return current is { Status: WorkflowStatus.Finished, SubStatus: WorkflowSubStatus.Cancelled };
        }, TimeSpan.FromSeconds(2));

        Assert.True(cycles.ActiveCount > 0, "The original execution cycle must still be active when drain starts.");

        await _orchestrator.DrainAsync(DrainTrigger.HostStopSignal);

        try { await runTask.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) when (!ex.IsFatal()) { /* runner may complete normally or surface OCE */ }

        var instance = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = client.WorkflowInstanceId });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, instance.SubStatus);

        var restarter = new RecordingRestarter();
        var scanner = ActivatorUtilities.CreateInstance<Elsa.Workflows.Runtime.Services.InterruptedRecoveryScanner>(scope.ServiceProvider, restarter);
        var requeued = await scanner.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal(0, requeued);
        Assert.Empty(restarter.RestartedIds);
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

    private static Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout) =>
        WaitUntilAsync(() => Task.FromResult(condition()), timeout);

    private sealed class RecordingRestarter : IWorkflowRestarter
    {
        public List<string> RestartedIds { get; } = new();

        public Task RestartWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            RestartedIds.Add(workflowInstanceId);
            return Task.CompletedTask;
        }
    }
}

/// <summary>Published definition used by <see cref="UserCancelDuringDrainTests"/>.</summary>
public class LongRunningObservableWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new ObservableActivity { DelayMs = 2000 };
    }
}

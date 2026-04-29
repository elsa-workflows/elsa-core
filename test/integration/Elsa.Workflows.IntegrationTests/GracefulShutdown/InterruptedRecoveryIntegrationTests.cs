using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Integration tests for the activation-time recovery of <see cref="WorkflowSubStatus.Interrupted"/> instances
/// (Phase 5 / US3). The scanner runs against the real <see cref="IWorkflowInstanceStore"/> — backed by the in-memory
/// store in this test setup — and exercises the same code path as production.
/// </summary>
public class InterruptedRecoveryIntegrationTests
{
    private readonly IServiceProvider _services;

    public InterruptedRecoveryIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime())
            .Build();
    }

    [Theory(DisplayName = "ScanAndRequeueAsync requeues 100% of Interrupted instances (SC-003)")]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task RequeuesAllInterruptedInstances(int count)
    {
        var fakeRestarter = new RecordingRestarter();
        using var scope = _services.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        await SeedInstancesAsync(instanceStore, count, WorkflowSubStatus.Interrupted, isExecuting: false);

        var scanner = ActivatorUtilities.CreateInstance<Elsa.Workflows.Runtime.Services.InterruptedRecoveryScanner>(scope.ServiceProvider, fakeRestarter);
        var requeued = await scanner.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal(count, requeued);
        Assert.Equal(count, fakeRestarter.RestartedIds.Count);
    }

    [Fact(DisplayName = "Scan ignores instances NOT in Interrupted sub-status (T071 — filter purity)")]
    public async Task FilterPurity()
    {
        var fakeRestarter = new RecordingRestarter();
        using var scope = _services.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();

        await SeedInstancesAsync(instanceStore, 3, WorkflowSubStatus.Interrupted, isExecuting: false, idPrefix: "interrupted-");
        await SeedInstancesAsync(instanceStore, 2, WorkflowSubStatus.Suspended, isExecuting: false, idPrefix: "suspended-");
        await SeedInstancesAsync(instanceStore, 2, WorkflowSubStatus.Cancelled, isExecuting: false, idPrefix: "cancelled-");
        await SeedInstancesAsync(instanceStore, 1, WorkflowSubStatus.Faulted, isExecuting: false, idPrefix: "faulted-");
        // Stale-executing instance — handled by the existing RestartInterruptedWorkflowsTask, not by the new scan.
        await SeedInstancesAsync(instanceStore, 1, WorkflowSubStatus.Executing, isExecuting: true, idPrefix: "stale-");

        var scanner = ActivatorUtilities.CreateInstance<Elsa.Workflows.Runtime.Services.InterruptedRecoveryScanner>(scope.ServiceProvider, fakeRestarter);
        var requeued = await scanner.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal(3, requeued);
        Assert.All(fakeRestarter.RestartedIds, id => Assert.StartsWith("interrupted-", id));
    }

    [Fact(DisplayName = "Scan does NOT touch IsExecuting=true instances (T069 — disjoint from RestartInterruptedWorkflowsTask)")]
    public async Task DisjointFromTimeoutBasedRecovery()
    {
        var fakeRestarter = new RecordingRestarter();
        using var scope = _services.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();

        // The two recovery paths use disjoint filters:
        //   - RestartInterruptedWorkflowsTask (recurring): IsExecuting=true AND UpdatedAt < threshold
        //   - RecoverInterruptedWorkflowsStartupTask (this scan): SubStatus = Interrupted (which has IsExecuting=false)
        await SeedInstancesAsync(instanceStore, 2, WorkflowSubStatus.Interrupted, isExecuting: false, idPrefix: "graceful-");
        await SeedInstancesAsync(instanceStore, 2, WorkflowSubStatus.Executing, isExecuting: true, idPrefix: "ungraceful-");

        var scanner = ActivatorUtilities.CreateInstance<Elsa.Workflows.Runtime.Services.InterruptedRecoveryScanner>(scope.ServiceProvider, fakeRestarter);
        var requeued = await scanner.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal(2, requeued);
        Assert.All(fakeRestarter.RestartedIds, id => Assert.StartsWith("graceful-", id));

        // The 'ungraceful-' instances remain in the store, untouched, available for the timeout-based task.
        var stillExecuting = await instanceStore.FindManyAsync(new WorkflowInstanceFilter { IsExecuting = true }, CancellationToken.None);
        Assert.Equal(2, stillExecuting.Count());
    }

    private static async Task SeedInstancesAsync(IWorkflowInstanceStore store, int count, WorkflowSubStatus subStatus, bool isExecuting, string idPrefix = "instance-")
    {
        for (var i = 0; i < count; i++)
        {
            await store.SaveAsync(new WorkflowInstance
            {
                Id = $"{idPrefix}{i}",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Running,
                SubStatus = subStatus,
                IsExecuting = isExecuting,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                WorkflowState = new Workflows.State.WorkflowState
                {
                    Id = $"{idPrefix}{i}",
                    DefinitionId = "def-1",
                    DefinitionVersionId = "ver-1",
                    Status = WorkflowStatus.Running,
                    SubStatus = subStatus,
                },
            }, CancellationToken.None);
        }
    }

    /// <summary>Captures restart calls without actually invoking the workflow runtime — keeps the integration test focused.</summary>
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

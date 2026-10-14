using System.Collections.Concurrent;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.KeyValues.Features;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>
/// What happens to a dispatched child workflow when the <c>DispatchWorkflow</c> activity waiting for it is cancelled in
/// the same burst that dispatched it, with the transactional outbox on. At that point the child exists only as a pending
/// dispatch in the outbox, owned by the parent's uncommitted state.
/// </summary>
public class TransactionalOutboxTests : IAsyncLifetime
{
    private readonly IServiceProvider _services;

    /// <summary>The child instance ID of every workflow definition dispatch written to the outbox, in order.</summary>
    private readonly ConcurrentQueue<string> _enqueuedInstanceIds = new();

    /// <summary>The child instance ID of every workflow definition dispatch the outbox delivered, in order.</summary>
    private readonly ConcurrentQueue<string> _deliveredInstanceIds = new();

    public TransactionalOutboxTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddWorkflow<BlockingChildWorkflow>()
            .AddWorkflow<FireAndForgetChildWorkflow>()
            .AddWorkflow<DispatchAndWaitBesideCompletingBranchWorkflow>()
            .AddWorkflow<FireAndForgetThenDispatchAndWaitBesideCompletingBranchWorkflow>()
            .ConfigureElsa(elsa =>
            {
                elsa.Configure<KeyValueFeature>();
                elsa.UseWorkflowRuntime(runtime =>
                {
                    runtime.WorkflowDispatchOutboxStore = sp =>
                        new RecordingWorkflowDispatchOutboxStore(ActivatorUtilities.CreateInstance<KeyValueWorkflowDispatchOutboxStore>(sp), _enqueuedInstanceIds);
                    runtime.InnerWorkflowDispatcher = sp =>
                        new RecordingWorkflowDispatcher(ActivatorUtilities.CreateInstance<BackgroundWorkflowDispatcher>(sp), _deliveredInstanceIds);
                });
            })
            // The test delivers the outbox itself, so what is pending after the parent's burst can be asserted first.
            .ConfigureServices(services => services.Configure<WorkflowDispatcherOptions>(options =>
            {
                options.UseTransactionalOutbox = true;
                options.ProcessOutboxAfterCommit = false;
            }))
            .Build();
    }

    public async Task InitializeAsync() => await _services.PopulateRegistriesAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "With the outbox on, cancelling a waiting DispatchWorkflow in the burst that dispatched its child withdraws the child's dispatch")]
    public async Task CancellingTheWaitingDispatchActivityInTheDispatchingBurst_WithdrawsTheChildDispatch()
    {
        // Act: the fork's other branch completes in the same burst, which cancels the waiting DispatchWorkflow.
        var parentId = await RunParentAsync(nameof(DispatchAndWaitBesideCompletingBranchWorkflow));

        // Assert: the child was dispatched in that burst, its pending dispatch has left the outbox, and the parent's
        // committed state no longer claims it.
        var childId = Assert.Single(_enqueuedInstanceIds);
        Assert.Empty(await FindPendingInstanceIdsAsync());
        Assert.False((await FindInstanceAsync(parentId))!.WorkflowState.HasWorkflowDispatchOutboxItems());

        // Assert the behavior: delivering the outbox never hands the child over for execution, so it never starts.
        await _services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Empty(_deliveredInstanceIds);
        Assert.Null(await FindInstanceAsync(childId));
    }

    [Fact(DisplayName = "With the outbox on, withdrawing a cancelled waiting dispatch leaves a fire-and-forget dispatch of the same burst pending")]
    public async Task CancellingTheWaitingDispatchActivityInTheDispatchingBurst_LeavesAFireAndForgetDispatchPending()
    {
        // Act
        await RunParentAsync(nameof(FireAndForgetThenDispatchAndWaitBesideCompletingBranchWorkflow));

        // Assert: both children were dispatched in the burst, the fire-and-forget one first, and only the waited-for
        // child's dispatch was withdrawn. Delivering the outbox hands over the fire-and-forget child and nothing else.
        var enqueued = _enqueuedInstanceIds.ToArray();
        Assert.Equal(2, enqueued.Length);
        var fireAndForgetChildId = enqueued[0];
        Assert.Equal(new[] { fireAndForgetChildId }, await FindPendingInstanceIdsAsync());

        await _services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Equal(new[] { fireAndForgetChildId }, _deliveredInstanceIds);
    }

    private async Task<string> RunParentAsync(string definitionId)
    {
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var response = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published)
        });

        Assert.Equal(WorkflowStatus.Finished, response.Status);
        Assert.Equal(WorkflowSubStatus.Finished, response.SubStatus);
        return response.WorkflowInstanceId;
    }

    private async Task<string?[]> FindPendingInstanceIdsAsync()
    {
        var items = await _services.GetRequiredService<IWorkflowDispatchOutboxStore>().FindManyAsync();
        return items.Select(x => x.WorkflowDefinitionCommand?.InstanceId).ToArray();
    }

    private async Task<Elsa.Workflows.Management.Entities.WorkflowInstance?> FindInstanceAsync(string instanceId) =>
        await _services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = instanceId });
}

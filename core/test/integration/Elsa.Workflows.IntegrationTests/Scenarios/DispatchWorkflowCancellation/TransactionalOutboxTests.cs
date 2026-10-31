using System.Collections.Concurrent;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.KeyValues.Features;
using Elsa.Testing.Shared;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.Features;
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
    private readonly ITestOutputHelper _testOutputHelper;
    private IServiceProvider _services;
    private bool _failOutboxDeletes;

    /// <summary>Every workflow definition dispatch written to the outbox, in order.</summary>
    private readonly ConcurrentQueue<EnqueuedChildDispatch> _enqueuedDispatches = new();

    /// <summary>The child instance ID of every workflow definition dispatch the outbox delivered, in order.</summary>
    private readonly ConcurrentQueue<string> _deliveredInstanceIds = new();

    public TransactionalOutboxTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _services = BuildServices();
    }

    private IServiceProvider BuildServices(Action<WorkflowsFeature>? configureWorkflows = null)
    {
        return new TestApplicationBuilder(_testOutputHelper)
            .AddWorkflow<BlockingChildWorkflow>()
            .AddWorkflow<FireAndForgetChildWorkflow>()
            .AddWorkflow<DispatchAndWaitBesideCompletingBranchWorkflow>()
            .AddWorkflow<FireAndForgetThenDispatchAndWaitBesideCompletingBranchWorkflow>()
            .AddWorkflow<DispatchAndWaitInLoopWorkflow>()
            .ConfigureElsa(elsa =>
            {
                elsa.Configure<KeyValueFeature>();
                elsa.UseWorkflows(workflows => configureWorkflows?.Invoke(workflows));
                elsa.UseWorkflowRuntime(runtime =>
                {
                    runtime.WorkflowDispatchOutboxStore = sp =>
                        new RecordingWorkflowDispatchOutboxStore(ActivatorUtilities.CreateInstance<KeyValueWorkflowDispatchOutboxStore>(sp), _enqueuedDispatches, () => _failOutboxDeletes);
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
        Assert.Single(_enqueuedDispatches);
        Assert.Empty(await FindPendingInstanceIdsAsync());
        Assert.False((await FindInstanceAsync(parentId))!.WorkflowState.HasWorkflowDispatchOutboxItems());

        // Assert the behavior: delivering the outbox never hands the child over for execution, so it never starts.
        await _services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Empty(_deliveredInstanceIds);
    }

    [Fact(DisplayName = "With the outbox on, withdrawing a cancelled waiting dispatch leaves a fire-and-forget dispatch of the same burst pending")]
    public async Task CancellingTheWaitingDispatchActivityInTheDispatchingBurst_LeavesAFireAndForgetDispatchPending()
    {
        // Act
        await RunParentAsync(nameof(FireAndForgetThenDispatchAndWaitBesideCompletingBranchWorkflow));

        // Assert: both children were dispatched in the burst, the fire-and-forget one first, and only the waited-for
        // child's dispatch was withdrawn. Delivering the outbox hands over the fire-and-forget child and nothing else.
        var enqueued = _enqueuedDispatches.ToArray();
        Assert.Equal(2, enqueued.Length);
        var fireAndForgetChildId = enqueued[0].InstanceId;
        Assert.Equal(new[] { fireAndForgetChildId }, await FindPendingInstanceIdsAsync());

        await _services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Equal(new[] { fireAndForgetChildId }, _deliveredInstanceIds);
    }

    [Fact(DisplayName = "With the outbox on, a withdrawal whose outbox delete fails still keeps the child from being delivered and does not fault the parent")]
    public async Task CancellingTheWaitingDispatchActivityInTheDispatchingBurst_WhenTheOutboxDeleteFails_StillWithdrawsTheChildDispatch()
    {
        _failOutboxDeletes = true;

        // Act
        var parentId = await RunParentAsync(nameof(DispatchAndWaitBesideCompletingBranchWorkflow));

        // Assert: the parent finished cleanly. The item is still stored, but the parent's committed state does not claim
        // it, so delivering the outbox does not hand the child over.
        var parent = (await FindInstanceAsync(parentId))!;
        Assert.Empty(parent.WorkflowState.Incidents);
        Assert.False(parent.WorkflowState.HasWorkflowDispatchOutboxItems());
        Assert.Equal(_enqueuedDispatches.Select(x => x.InstanceId), await FindPendingInstanceIdsAsync());

        await _services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Empty(_deliveredInstanceIds);
    }

    [Fact(DisplayName = "With the outbox on, a pending dispatch the parent already committed in the same burst is still withdrawn while undelivered")]
    public async Task CancellingTheWaitingDispatchActivityAfterAMidBurstCommit_WithdrawsTheUndeliveredChildDispatch()
    {
        // Arrange: commit after every activity, so the parent commits the dispatch before the fork cancels it. The outbox is
        // only delivered by the test, so the committed item is still undelivered when the cancellation runs.
        _services = BuildServices(workflows => workflows.WithDefaultActivityCommitStrategy(new ExecutedActivityStrategy()));
        await _services.PopulateRegistriesAsync();

        // Act
        await RunParentAsync(nameof(DispatchAndWaitBesideCompletingBranchWorkflow));

        // Assert
        Assert.Single(_enqueuedDispatches);
        Assert.Empty(await FindPendingInstanceIdsAsync());

        await _services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Empty(_deliveredInstanceIds);
    }

    [Fact(DisplayName = "With the outbox on, a DispatchWorkflow that dispatches again in the same burst withdraws only the cancelled iteration's child")]
    public async Task CancellingOneIterationOfARepeatedDispatchInTheDispatchingBurst_WithdrawsOnlyThatChild()
    {
        // Act: the last iteration's dispatch is cancelled, the other one keeps waiting.
        await RunParentAsync(nameof(DispatchAndWaitInLoopWorkflow), WorkflowStatus.Running, WorkflowSubStatus.Suspended);

        // Assert: the same activity dispatched two waited-for children in the burst. Correlation identifies which
        // iteration each child belongs to, so withdrawing the still-waiting iteration would fail these assertions.
        var enqueued = _enqueuedDispatches.ToArray();
        Assert.Equal(2, enqueued.Length);
        var stillWaitingChildId = Assert.Single(enqueued, x => x.CorrelationId == DispatchAndWaitInLoopWorkflow.StillWaitingIteration.ToString()).InstanceId;
        var cancelledChildId = Assert.Single(enqueued, x => x.CorrelationId == DispatchAndWaitInLoopWorkflow.LastIteration.ToString()).InstanceId;
        var pending = await FindPendingInstanceIdsAsync();
        Assert.Equal(new[] { stillWaitingChildId }, pending);
        Assert.DoesNotContain(cancelledChildId, pending);

        await _services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Equal(new[] { stillWaitingChildId }, _deliveredInstanceIds);
        Assert.DoesNotContain(cancelledChildId, _deliveredInstanceIds);
    }

    private async Task<string> RunParentAsync(string definitionId, WorkflowStatus expectedStatus = WorkflowStatus.Finished, WorkflowSubStatus expectedSubStatus = WorkflowSubStatus.Finished)
    {
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var response = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published)
        });

        Assert.Equal(expectedStatus, response.Status);
        Assert.Equal(expectedSubStatus, response.SubStatus);
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

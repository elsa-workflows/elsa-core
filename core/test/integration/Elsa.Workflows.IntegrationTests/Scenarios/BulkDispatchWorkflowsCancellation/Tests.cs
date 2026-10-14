using System.Collections.Concurrent;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// What happens to bulk-dispatched child workflows when the <c>BulkDispatchWorkflows</c> activity waiting for them is cancelled.
/// </summary>
public class Tests : IAsyncLifetime
{
    private static readonly TimeSpan ChildTimeout = TimeSpan.FromSeconds(10);

    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IServiceProvider _services;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly BackgroundCommandSenderHostedService _commandProcessor;

    /// <summary>Every instance whose cancellation was dispatched, in order.</summary>
    private readonly ConcurrentQueue<string> _cancellationTargets = new();

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddWorkflow<BlockingChildWorkflow>()
            .AddWorkflow<FireAndForgetChildWorkflow>()
            .AddWorkflow<SecondBulkChildWorkflow>()
            .AddWorkflow<BulkDispatchAndWaitInForkWorkflow>()
            .AddWorkflow<BulkDispatchAndWaitBesideSiblingDispatchesWorkflow>()
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime(runtime => runtime.WorkflowCancellationDispatcher = sp =>
                new RecordingWorkflowCancellationDispatcher(ActivatorUtilities.CreateInstance<BackgroundWorkflowCancellationDispatcher>(sp), _cancellationTargets)))
            .Build();
        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();

        // Dispatch and cancellation both go through the background command sender, which nothing starts in a test host.
        _commandProcessor = _services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
    }

    public async Task InitializeAsync()
    {
        await _services.PopulateRegistriesAsync();
        await _commandProcessor.StartAsync(CancellationToken.None);
    }

    public Task DisposeAsync() => _commandProcessor.StopAsync(CancellationToken.None);

    [Fact(DisplayName = "Cancelling a waiting BulkDispatchWorkflows activity cancels the child workflows it dispatched")]
    public async Task CancellingTheWaitingBulkDispatchActivity_CancelsTheChildWorkflows()
    {
        // Arrange: the children are dispatched and suspended before the parent moves on.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), BulkDispatchAndWaitInForkWorkflow.ChildCount, IsSuspended);

        // Act: the other fork branch completes, so the fork completes and cancels the waiting BulkDispatchWorkflows.
        var resumed = await ResumeProceedAsync(parentId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);

        // Assert the behavior: every child the cancelled activity was waiting for is cancelled too. Give an asynchronous
        // cancellation the same time a dispatch gets; what is asserted is the status the children end up in.
        var children = await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), BulkDispatchAndWaitInForkWorkflow.ChildCount, IsFinished, throwOnTimeout: false);

        Assert.All(children, child =>
        {
            Assert.Equal(WorkflowStatus.Finished, child.Status);
            Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus);
        });
    }

    [Fact(DisplayName = "Cancelling a waiting BulkDispatchWorkflows activity leaves fire-and-forget and other bulk dispatches of the same parent running")]
    public async Task CancellingTheWaitingBulkDispatchActivity_LeavesSiblingDispatchesRunning()
    {
        // Arrange: the parent has dispatched one child fire-and-forget, and two bulk activities are waiting for their own children.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitBesideSiblingDispatchesWorkflow));
        await WaitForChildrenAsync(parentId, nameof(FireAndForgetChildWorkflow), 1, IsSuspended);
        await WaitForChildrenAsync(parentId, nameof(SecondBulkChildWorkflow), 1, IsSuspended);
        var waitedChildIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), BulkDispatchAndWaitInForkWorkflow.ChildCount, IsSuspended)).Select(x => x.Id).Order().ToList();

        // Act: cancel the first bulk activity. The second one keeps the parent waiting.
        var resumed = await ResumeProceedAsync(parentId);
        Assert.Equal(WorkflowSubStatus.Suspended, resumed.SubStatus);

        // Assert: the cancelled activity dispatched a cancellation for each child it was waiting for and nothing else,
        // and those cancellations land.
        Assert.Equal(waitedChildIds, _cancellationTargets.Order());
        var waitedChildren = await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), BulkDispatchAndWaitInForkWorkflow.ChildCount, IsFinished);
        Assert.All(waitedChildren, child => Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus));
    }

    private static bool IsSuspended(WorkflowState state) => state.SubStatus == WorkflowSubStatus.Suspended;
    private static bool IsFinished(WorkflowState state) => state.Status == WorkflowStatus.Finished;

    private async Task<string> StartParentAsync(string definitionId)
    {
        var response = await _services.GetRequiredService<IWorkflowStarter>().StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published)
        });

        Assert.False(response.CannotStart);
        return Assert.IsType<string>(response.WorkflowInstanceId);
    }

    /// <summary>Resumes the parent's proceed branch, which completes the inner fork and cancels its waiting bulk activity.</summary>
    private async Task<RunWorkflowInstanceResponse> ResumeProceedAsync(string parentId)
    {
        var client = await _workflowRuntime.CreateClientAsync(parentId);
        var bookmark = (await client.ExportStateAsync()).Bookmarks.Single(x => x.ActivityId == BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        return await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = bookmark.Id });
    }

    /// <summary>
    /// Polls for the children of the definition the parent dispatched until there are the expected number and all of them
    /// satisfy the condition. On timeout it throws, or, when the wait is itself what the test is about, returns the last
    /// states observed so the test asserts on them.
    /// </summary>
    private async Task<IReadOnlyList<WorkflowState>> WaitForChildrenAsync(string parentId, string definitionId, int expectedCount, Func<WorkflowState, bool> condition, bool throwOnTimeout = true)
    {
        var store = _services.GetRequiredService<IWorkflowInstanceStore>();
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = definitionId,
            ParentWorkflowInstanceIds = [parentId]
        };
        var timeoutAt = DateTimeOffset.UtcNow + ChildTimeout;
        IReadOnlyList<WorkflowState> last = [];

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            last = (await store.FindManyAsync(filter)).Select(x => x.WorkflowState).ToList();
            Assert.True(last.Count <= expectedCount, $"Expected at most {expectedCount} {definitionId} children, found {last.Count}.");

            if (last.Count == expectedCount && last.All(condition))
                return last;

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        _testOutputHelper.WriteLine($"{definitionId} children after {ChildTimeout}: [{string.Join(", ", last.Select(x => $"{x.Id} {x.Status}/{x.SubStatus}"))}]");

        if (throwOnTimeout || last.Count != expectedCount)
            throw new TimeoutException($"The {expectedCount} dispatched {definitionId} children did not reach the expected state within {ChildTimeout}. Last observed: [{string.Join(", ", last.Select(x => $"{x.Status}/{x.SubStatus}"))}].");

        return last;
    }
}

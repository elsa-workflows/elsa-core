using System.Collections.Concurrent;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
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

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>
/// What happens to a dispatched child workflow when the <c>DispatchWorkflow</c> activity waiting for it is cancelled.
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
            .AddWorkflow<FinishingChildWorkflow>()
            .AddWorkflow<DispatchAndWaitInForkWorkflow>()
            .AddWorkflow<DispatchAndWaitBesideFireAndForgetWorkflow>()
            .AddWorkflow<DispatchAndWaitForFinishingChildWorkflow>()
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

    [Fact(DisplayName = "Cancelling a waiting DispatchWorkflow activity cancels the child workflow it dispatched")]
    public async Task CancellingTheWaitingDispatchActivity_CancelsTheChildWorkflow()
    {
        // Arrange: the child is dispatched and suspended before the parent moves on.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitInForkWorkflow));
        await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsSuspended);

        // Act: the other fork branch completes, so the fork completes and cancels the waiting DispatchWorkflow.
        await ProceedParentAsync(parentId);

        // Assert the behavior: the child the cancelled activity was waiting for is cancelled too. Give an asynchronous
        // cancellation the same time a dispatch gets; what is asserted is the status the child ends up in.
        var child = await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsFinished, throwOnTimeout: false);

        Assert.Equal(WorkflowStatus.Finished, child.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus);
    }

    [Fact(DisplayName = "Cancelling a waiting DispatchWorkflow activity leaves a fire-and-forget sibling dispatch running")]
    public async Task CancellingTheWaitingDispatchActivity_LeavesAFireAndForgetSiblingRunning()
    {
        // Arrange: the parent has dispatched one child fire-and-forget and is waiting for another.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitBesideFireAndForgetWorkflow));
        await WaitForChildAsync(parentId, nameof(FireAndForgetChildWorkflow), IsSuspended);
        var waitedChildId = (await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsSuspended)).Id;

        // Act
        await ProceedParentAsync(parentId);

        // Assert: the cancelled activity dispatched a cancellation for the waited-for child and nothing else, and that
        // cancellation lands.
        Assert.Equal(new[] { waitedChildId }, _cancellationTargets);
        var waitedChild = await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsFinished);
        Assert.Equal(WorkflowSubStatus.Cancelled, waitedChild.SubStatus);
    }

    [Fact(DisplayName = "Cancelling a waiting DispatchWorkflow activity whose child already finished changes nothing")]
    public async Task CancellingTheWaitingDispatchActivity_WhenTheChildAlreadyFinished_IsANoOp()
    {
        // Arrange: the child has dispatched a fire-and-forget child of its own, and then finished. The test host runs no
        // bookmark queue worker, so the child's completion is not delivered and the parent's DispatchWorkflow keeps waiting.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitForFinishingChildWorkflow));
        var childId = (await WaitForChildAsync(parentId, nameof(FinishingChildWorkflow), IsSuspended)).Id;
        await WaitForChildAsync(childId, nameof(FireAndForgetChildWorkflow), IsSuspended);

        var finished = await ResumeAsync(childId, FinishingChildWorkflow.FinishActivityId);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Contains((await ExportStateAsync(parentId)).Bookmarks, x => x.ActivityId == DispatchAndWaitInForkWorkflow.DispatchActivityId);

        // Act
        await ProceedParentAsync(parentId);

        // Assert: the parent finished without an incident, and no cancellation was dispatched at all, so neither the
        // finished child nor its own child is touched.
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);
        Assert.Empty(_cancellationTargets);
    }

    [Fact(DisplayName = "Control: cancelling the parent workflow through the cancellation service cancels the dispatched child")]
    public async Task Control_CancellingTheParentWorkflow_CancelsTheChildWorkflow()
    {
        // Arrange: the same parent and child, with the child dispatched and suspended.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitInForkWorkflow));
        await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsSuspended);

        // Act
        Assert.True(await _services.GetRequiredService<IWorkflowCancellationService>().CancelWorkflowAsync(parentId));

        // Assert: the cascade by parent instance id reaches the child.
        var child = await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsFinished);

        Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus);
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

    /// <summary>
    /// Resumes the parent's second fork branch, which completes the fork and cancels its waiting DispatchWorkflow, and
    /// asserts the setup: the parent finished, and the DispatchWorkflow activity's bookmark went with it.
    /// </summary>
    private async Task ProceedParentAsync(string parentId)
    {
        var resumed = await ResumeAsync(parentId, DispatchAndWaitInForkWorkflow.ProceedActivityId);

        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Bookmarks);
    }

    private async Task<RunWorkflowInstanceResponse> ResumeAsync(string instanceId, string activityId)
    {
        var bookmark = (await ExportStateAsync(instanceId)).Bookmarks.Single(x => x.ActivityId == activityId);
        var client = await _workflowRuntime.CreateClientAsync(instanceId);
        return await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = bookmark.Id });
    }

    private async Task<WorkflowState> ExportStateAsync(string instanceId)
    {
        var client = await _workflowRuntime.CreateClientAsync(instanceId);
        return await client.ExportStateAsync();
    }

    /// <summary>
    /// Polls for the single child of the definition the parent dispatched until it satisfies the condition. On timeout
    /// it throws, or, when the wait is itself what the test is about, returns the last state observed so the test
    /// asserts on it.
    /// </summary>
    private async Task<WorkflowState> WaitForChildAsync(string parentId, string definitionId, Func<WorkflowState, bool> condition, bool throwOnTimeout = true)
    {
        var store = _services.GetRequiredService<IWorkflowInstanceStore>();
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = definitionId,
            ParentWorkflowInstanceIds = [parentId]
        };
        var timeoutAt = DateTimeOffset.UtcNow + ChildTimeout;
        WorkflowState? last = null;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var children = (await store.FindManyAsync(filter)).ToList();
            Assert.True(children.Count <= 1, $"Expected at most one {definitionId} child, found {children.Count}.");
            last = children.SingleOrDefault()?.WorkflowState;

            if (last != null && condition(last))
                return last;

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        _testOutputHelper.WriteLine($"{definitionId} child after {ChildTimeout}: {(last == null ? "(none)" : $"{last.Id} {last.Status}/{last.SubStatus}, bookmarks [{string.Join(", ", last.Bookmarks.Select(x => x.ActivityId))}]")}");

        if (throwOnTimeout || last == null)
            throw new TimeoutException($"The dispatched {definitionId} child did not reach the expected state within {ChildTimeout}. Last observed: {(last == null ? "(none)" : $"{last.Status}/{last.SubStatus}")}.");

        return last;
    }
}

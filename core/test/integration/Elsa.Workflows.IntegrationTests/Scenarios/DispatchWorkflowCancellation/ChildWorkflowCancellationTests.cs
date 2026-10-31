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
/// Shared host and helpers for tests about what happens to dispatched child workflows when the activity waiting for them
/// is cancelled. The host records every cancellation it dispatches and runs the background command sender that dispatch
/// and cancellation go through.
/// </summary>
public abstract class ChildWorkflowCancellationTests : IAsyncLifetime
{
    private static readonly TimeSpan ChildTimeout = TimeSpan.FromSeconds(30);

    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly BackgroundCommandSenderHostedService _commandProcessor;

    protected ChildWorkflowCancellationTests(ITestOutputHelper testOutputHelper, Func<TestApplicationBuilder, TestApplicationBuilder> addWorkflows)
    {
        _testOutputHelper = testOutputHelper;
        Services = addWorkflows(new TestApplicationBuilder(testOutputHelper))
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime(runtime => runtime.WorkflowCancellationDispatcher = sp =>
                new RecordingWorkflowCancellationDispatcher(ActivatorUtilities.CreateInstance<BackgroundWorkflowCancellationDispatcher>(sp), CancellationTargets)))
            .Build();
        _workflowRuntime = Services.GetRequiredService<IWorkflowRuntime>();

        // Dispatch and cancellation both go through the background command sender, which nothing starts in a test host.
        _commandProcessor = Services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
    }

    protected IServiceProvider Services { get; }

    /// <summary>Every instance whose cancellation was dispatched, in order.</summary>
    protected ConcurrentQueue<string> CancellationTargets { get; } = new();

    public async Task InitializeAsync()
    {
        await Services.PopulateRegistriesAsync();
        await _commandProcessor.StartAsync(CancellationToken.None);
    }

    public Task DisposeAsync() => _commandProcessor.StopAsync(CancellationToken.None);

    protected static bool IsSuspended(WorkflowState state) => state.SubStatus == WorkflowSubStatus.Suspended;
    protected static bool IsFinished(WorkflowState state) => state.Status == WorkflowStatus.Finished;

    protected async Task<string> StartParentAsync(string definitionId)
    {
        var response = await Services.GetRequiredService<IWorkflowStarter>().StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published)
        });

        Assert.False(response.CannotStart);
        return Assert.IsType<string>(response.WorkflowInstanceId);
    }

    protected async Task<RunWorkflowInstanceResponse> ResumeAsync(string instanceId, string activityId)
    {
        var bookmark = (await ExportStateAsync(instanceId)).Bookmarks.Single(x => x.ActivityId == activityId);
        var client = await _workflowRuntime.CreateClientAsync(instanceId);
        return await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = bookmark.Id });
    }

    protected async Task<WorkflowState> ExportStateAsync(string instanceId)
    {
        var client = await _workflowRuntime.CreateClientAsync(instanceId);
        return await client.ExportStateAsync();
    }

    /// <summary>Delivers pending transactional-outbox child dispatches.</summary>
    protected Task DeliverOutboxAsync() => Services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

    /// <summary>Polls for the single child of the definition the parent dispatched; see <see cref="WaitForChildrenAsync"/>.</summary>
    protected async Task<WorkflowState> WaitForChildAsync(string parentId, string definitionId, Func<WorkflowState, bool> condition, bool throwOnTimeout = true) =>
        (await WaitForChildrenAsync(parentId, definitionId, 1, condition, throwOnTimeout)).Single();

    /// <summary>
    /// Polls for the children of the definition the parent dispatched until there are the expected number and all of them
    /// satisfy the condition. On timeout it throws, or, when the wait is itself what the test is about, returns the last
    /// states observed so the test asserts on them.
    /// </summary>
    protected async Task<IReadOnlyList<WorkflowState>> WaitForChildrenAsync(string parentId, string definitionId, int expectedCount, Func<WorkflowState, bool> condition, bool throwOnTimeout = true)
    {
        var store = Services.GetRequiredService<IWorkflowInstanceStore>();
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

        _testOutputHelper.WriteLine($"{definitionId} children after {ChildTimeout}: [{string.Join(", ", last.Select(x => $"{x.Id} {x.Status}/{x.SubStatus}, bookmarks [{string.Join(", ", x.Bookmarks.Select(b => b.ActivityId))}]"))}]");

        if (throwOnTimeout || last.Count != expectedCount)
            throw new TimeoutException($"The {expectedCount} dispatched {definitionId} children did not reach the expected state within {ChildTimeout}. Last observed: [{string.Join(", ", last.Select(x => $"{x.Status}/{x.SubStatus}"))}].");

        return last;
    }
}

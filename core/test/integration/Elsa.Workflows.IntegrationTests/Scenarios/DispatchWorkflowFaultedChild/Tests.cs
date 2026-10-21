using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowFaultedChild;

/// <summary>
/// A flowchart waiting on <c>DispatchWorkflow</c> for a child that faults: the activity still completes on <c>Done</c>, so
/// the flowchart carries on. A BPMN call activity routes that failure instead, but only through its own host.
/// </summary>
/// <remarks>
/// The component-test server keeps a faulted workflow suspended with an incident rather than finishing it, so this
/// lives here, under the default fault strategy. The child runs through the background command sender; the test waits
/// for the child's final commit, by which point its completion is in the bookmark queue, and then drains that queue
/// once, which resumes the parent in line.
/// </remarks>
public class Tests : IAsyncLifetime
{
    private static readonly TimeSpan ChildTimeout = TimeSpan.FromSeconds(30);

    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly TaskCompletionSource<WorkflowInstance> _childFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IServiceProvider _services;
    private readonly BackgroundCommandSenderHostedService _commandSender;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddWorkflow<FaultingChildWorkflow>()
            .AddWorkflow<DispatchFaultingChildWorkflow>()
            .ConfigureServices(services => services.AddNotificationHandler<ChildFinishedHandler, WorkflowStateCommitted>(_ => new(_childFinished)))
            .Build();
        _commandSender = _services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
    }

    public async Task InitializeAsync()
    {
        await _services.PopulateRegistriesAsync();
        await _commandSender.StartAsync(CancellationToken.None);
    }

    public Task DisposeAsync() => _commandSender.StopAsync(CancellationToken.None);

    [Fact(DisplayName = "A flowchart continues on Done after the child a waiting DispatchWorkflow dispatched faults")]
    public async Task FaultedChild_ParentContinuesOnDone()
    {
        // Act: the parent dispatches the child and suspends.
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var started = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(DispatchFaultingChildWorkflow), VersionOptions.Published)
        });
        Assert.Equal(WorkflowSubStatus.Suspended, started.SubStatus);

        // Act: the child faults, and its completion resumes the parent.
        var child = await _childFinished.Task.WaitAsync(ChildTimeout);

        using (var scope = _services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>().ProcessAsync();
        }

        // Assert: the child faulted, and the parent still moved on past DispatchWorkflow and finished without an incident.
        var parent = Assert.IsType<WorkflowInstance>(await _services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = started.WorkflowInstanceId }));

        Assert.Equal(WorkflowSubStatus.Faulted, child.SubStatus);
        Assert.Equal(WorkflowSubStatus.Finished, parent.SubStatus);
        Assert.Empty(parent.WorkflowState.Incidents);
        Assert.Contains(DispatchFaultingChildWorkflow.ParentCompletedText, _capturingTextWriter.Lines);
    }

    /// <summary>
    /// Signals the first child to commit a finished state. The parent's completion is enqueued before that commit, so
    /// once it fires the bookmark queue holds the item that resumes the parent.
    /// </summary>
    private sealed class ChildFinishedHandler(TaskCompletionSource<WorkflowInstance> childFinished) : INotificationHandler<WorkflowStateCommitted>
    {
        public Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowInstance.ParentWorkflowInstanceId is not null && notification.WorkflowState.Status == WorkflowStatus.Finished)
            {
                childFinished.TrySetResult(notification.WorkflowInstance);
            }

            return Task.CompletedTask;
        }
    }
}

using Bpmn.Semantics;
using Elsa.Bpmn.Exceptions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime.BpmnRuntimeTestHost;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime;

/// <summary>
/// A BPMN call activity, which the binder binds to <c>DispatchWorkflow</c> on its <c>calledElement</c>, run end to end
/// against a real child workflow: waiting for the child, fire-and-forget, a child that faults, and a child that is cancelled.
/// </summary>
/// <remarks>
/// The child blocks on an event of its own, so "the parent is waiting for it" is a state the test can observe before
/// it lets the child finish, rather than a race the child happens to win.
/// </remarks>
public class BpmnCallActivityTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private const string ChildDefinitionId = "ship-order";
    private const string ChildReleaseEvent = "ShipmentHandedOver";
    private const string FailingChildDefinitionId = "ship-order-failing";

    private readonly BpmnRuntimeTestHost _host = new(testOutputHelper);

    public async Task InitializeAsync()
    {
        await _host.InitializeAsync();
        await _host.PublishAsync(ChildDefinitionId, new Sequence
        {
            Activities =
            {
                new Event(ChildReleaseEvent),
                new WriteLine("Shipped")
            }
        });
        await _host.PublishAsync(FailingChildDefinitionId, Fault.Create("SHIP-01", "Shipping", "Business", "The carrier rejected the shipment."));
    }

    public Task DisposeAsync() => _host.DisposeAsync();

    [Fact(DisplayName = "A call activity waits for the called workflow to finish before the process moves on")]
    public async Task CallActivity_WaitsForTheChildToFinish()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("call-activity-wait.bpmn");
        var ship = WorkActivityId("place-order", "Ship");
        var confirm = WorkActivityId("place-order", "Confirm");

        // Act: the parent reaches the call activity and dispatches the child.
        var parent = await _host.StartAsync(definitionId);
        var child = await WaitForChildAsync(parent, IsSuspended);

        // Assert: the parent is waiting on the call activity, and has not moved past it.
        var waiting = await _host.StateOfAsync(parent);
        Assert.True(IsSuspended(waiting));
        Assert.Equal(ship, Assert.Single(waiting.Bookmarks).ActivityId);
        Assert.False((await _host.ActivityStatusesOfAsync(parent)).ContainsKey(confirm));

        // Act: the child is allowed to finish.
        await _host.PublishEventAsync(ChildReleaseEvent);

        // Assert: the child finished, and only then did the parent move past the call activity and finish.
        Assert.Equal(WorkflowSubStatus.Finished, (await _host.StateOfAsync(child)).SubStatus);

        var finished = await _host.WaitForAsync(parent, IsFinished);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);

        var statuses = await _host.ActivityStatusesOfAsync(parent);
        Assert.Equal(ActivityStatus.Completed, statuses[ship]);
        Assert.Equal(ActivityStatus.Completed, statuses[confirm]);
    }

    [Fact(DisplayName = "A fire-and-forget call activity dispatches the called workflow and moves on without waiting for it")]
    public async Task FireAndForgetCallActivity_DoesNotWaitForTheChild()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("call-activity-fire-and-forget.bpmn");

        // Act
        var parent = await _host.StartAsync(definitionId);

        // Assert: the parent finished straight through the call activity...
        var finished = await _host.StateOfAsync(parent);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);
        Assert.Equal(ActivityStatus.Completed, (await _host.ActivityStatusesOfAsync(parent))[WorkActivityId("notify-order", "Confirm")]);

        // ...while the child it dispatched is still running on its own.
        var child = await WaitForChildAsync(parent, IsSuspended);
        Assert.True(IsSuspended(await _host.StateOfAsync(child)));
    }

    [Fact(DisplayName = "A call activity whose called workflow faults routes the error boundary on the call activity instead of its normal flow")]
    public async Task CallActivity_ChildFaults_RoutesTheErrorBoundary()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("call-activity-failing-child.bpmn");

        // Act
        var parent = await _host.StartAsync(definitionId);
        var finished = await _host.WaitForAsync(parent, IsFinished);

        // Assert: the child faulted, and the parent took the error boundary's path rather than the call activity's
        // normal outgoing flow, then finished cleanly.
        Assert.Equal(WorkflowSubStatus.Faulted, (await FailingChildOfAsync(parent)).SubStatus);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);

        var statuses = await _host.ActivityStatusesOfAsync(parent);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId("place-risky-order", "CompensateCustomer")]);
        Assert.False(statuses.ContainsKey(WorkActivityId("place-risky-order", "Confirm")));
    }

    [Fact(DisplayName = "A call activity whose called workflow faults, with no error catcher, faults the process")]
    public async Task CallActivity_ChildFaults_WithoutACatcher_FaultsTheProcess()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("call-activity-failing-child-uncaught.bpmn");

        // Act
        var parent = await _host.StartAsync(definitionId);
        var finished = await _host.WaitForAsync(parent, IsFinished);

        // Assert: nothing caught the child's failure, so the process faulted with the call activity's fault code and
        // never took the normal outgoing flow.
        Assert.Equal(WorkflowSubStatus.Faulted, (await FailingChildOfAsync(parent)).SubStatus);
        Assert.Equal(WorkflowSubStatus.Faulted, finished.SubStatus);
        var incident = Assert.Single(finished.Incidents);
        Assert.Equal(typeof(BpmnScopeFaultException), incident.Exception?.Type);
        Assert.StartsWith(BpmnInterpreter.CallActivityFaultedFaultCode, incident.Message);
        Assert.False((await _host.ActivityStatusesOfAsync(parent)).ContainsKey(WorkActivityId("place-unguarded-order", "Confirm")));
    }

    [Fact(DisplayName = "A call activity whose called workflow is cancelled reports the cancelled outcome, which faults the process with no catcher")]
    public async Task CallActivity_ChildCancelled_ReportsTheCancelledOutcome()
    {
        // Arrange: the parent is waiting on the call activity for its suspended child.
        var definitionId = await _host.ImportAndPublishAsync("call-activity-wait.bpmn");
        var parent = await _host.StartAsync(definitionId);
        var child = await WaitForChildAsync(parent, IsSuspended);

        // Act: cancel the child, the way the cancellation API does.
        Assert.True(await _host.Services.GetRequiredService<IWorkflowCancellationService>().CancelWorkflowAsync(child));

        // Assert: the child was cancelled, and the parent resumed with the call activity's cancelled outcome. Nothing
        // catches it, so the process faulted with the cancelled fault code and never took the normal outgoing flow.
        Assert.Equal(WorkflowSubStatus.Cancelled, (await _host.WaitForAsync(child, IsFinished)).SubStatus);

        var finished = await _host.WaitForAsync(parent, IsFinished);
        Assert.Equal(WorkflowSubStatus.Faulted, finished.SubStatus);
        var incident = Assert.Single(finished.Incidents);
        Assert.StartsWith(BpmnInterpreter.CallActivityCancelledFaultCode, incident.Message);
        Assert.False((await _host.ActivityStatusesOfAsync(parent)).ContainsKey(WorkActivityId("place-order", "Confirm")));
    }

    /// <summary>The failing child the parent called. It finished before the parent could resume, so it is already saved.</summary>
    private async Task<WorkflowState> FailingChildOfAsync(string parentId) =>
        (await _host.WaitForInstanceAsync(
            instance => instance.DefinitionId == FailingChildDefinitionId && instance.ParentWorkflowInstanceId == parentId,
            $"A '{FailingChildDefinitionId}' child of {parentId}")).WorkflowState;

    private async Task<string> WaitForChildAsync(string parentId, Func<WorkflowState, bool> condition)
    {
        var child = await _host.WaitForInstanceAsync(
            instance => instance.DefinitionId == ChildDefinitionId && instance.ParentWorkflowInstanceId == parentId && condition(instance.WorkflowState),
            $"A '{ChildDefinitionId}' child of {parentId}");

        return child.Id;
    }
}

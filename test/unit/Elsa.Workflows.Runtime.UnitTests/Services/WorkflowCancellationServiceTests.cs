using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Elsa.Workflows.State;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowCancellationServiceTests
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
    private readonly IWorkflowInstanceStore _workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
    private readonly ISystemClock _systemClock = Substitute.For<ISystemClock>();
    private readonly IWorkflowCancellationDispatcher _dispatcher = Substitute.For<IWorkflowCancellationDispatcher>();

    [Fact]
    public async Task CancelWorkflowsAsync_MarksNonFinishedInstancesAsCancellingBeforeDispatchingCancellation()
    {
        var now = new DateTimeOffset(2026, 5, 22, 17, 0, 0, TimeSpan.Zero);
        var events = new List<string>();
        var suspendedInstance = CreateInstance("workflow-1", WorkflowStatus.Running, WorkflowSubStatus.Suspended);
        var finishedInstance = CreateInstance("workflow-2", WorkflowStatus.Finished, WorkflowSubStatus.Finished);
        var instances = new[] { suspendedInstance, finishedInstance };
        var service = CreateService();

        _systemClock.UtcNow.Returns(now);
        _workflowInstanceStore
            .FindManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var filter = callInfo.ArgAt<WorkflowInstanceFilter>(0);
                var matchesRequestedIds = filter.Ids?.Any() == true;
                IEnumerable<WorkflowInstance> result = matchesRequestedIds ? instances : Array.Empty<WorkflowInstance>();
                return new ValueTask<IEnumerable<WorkflowInstance>>(result);
            });
        _workflowInstanceStore
            .SaveManyAsync(Arg.Any<IEnumerable<WorkflowInstance>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                events.Add("save");
                return ValueTask.CompletedTask;
            });
        _dispatcher
            .DispatchAsync(Arg.Any<DispatchCancelWorkflowRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                events.Add("dispatch");
                return Task.FromResult(new DispatchCancelWorkflowsResponse());
            });

        var count = await service.CancelWorkflowsAsync(new[] { "workflow-1", "workflow-2" });

        Assert.Equal(1, count);
        Assert.Equal(new[] { "save", "dispatch" }, events);
        Assert.Equal(WorkflowStatus.Running, suspendedInstance.Status);
        Assert.Equal(WorkflowSubStatus.Cancelling, suspendedInstance.SubStatus);
        Assert.Equal(now, suspendedInstance.UpdatedAt);
        Assert.Equal(WorkflowStatus.Running, suspendedInstance.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Cancelling, suspendedInstance.WorkflowState.SubStatus);
        Assert.Equal(now, suspendedInstance.WorkflowState.UpdatedAt);
        Assert.Equal(WorkflowSubStatus.Finished, finishedInstance.SubStatus);

        await _workflowInstanceStore.Received(1).SaveManyAsync(
            Arg.Is<IEnumerable<WorkflowInstance>>(items =>
                items.Count() == 1 &&
                items.Single().Id == suspendedInstance.Id),
            Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<DispatchCancelWorkflowRequest>(request => request.WorkflowInstanceId == suspendedInstance.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelWorkflowAsync_ReturnsFalseWithoutDispatching_WhenInstanceDoesNotExist()
    {
        var service = CreateService();
        _workflowInstanceStore
            .FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>((WorkflowInstance?)null));

        var result = await service.CancelWorkflowAsync("missing-workflow");

        Assert.False(result);
        await _workflowInstanceStore.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowInstance>>(), Arg.Any<CancellationToken>());
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<DispatchCancelWorkflowRequest>(), Arg.Any<CancellationToken>());
    }

    private WorkflowCancellationService CreateService() => new(_workflowDefinitionService, _workflowInstanceStore, _systemClock, _dispatcher);

    private static WorkflowInstance CreateInstance(string id, WorkflowStatus status, WorkflowSubStatus subStatus)
    {
        var workflowState = new WorkflowState
        {
            Id = id,
            Status = status,
            SubStatus = subStatus
        };

        return new()
        {
            Id = id,
            DefinitionId = "definition-1",
            DefinitionVersionId = "definition-version-1",
            Status = status,
            SubStatus = subStatus,
            WorkflowState = workflowState
        };
    }
}

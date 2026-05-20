using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowDispatchOutboxProcessorTests
{
    private readonly IWorkflowDispatchOutboxStore _store = Substitute.For<IWorkflowDispatchOutboxStore>();
    private readonly IWorkflowInstanceStore _workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
    private readonly ICommandSender _commandSender = Substitute.For<ICommandSender>();
    private readonly WorkflowDispatchOutboxProcessor _processor;

    public WorkflowDispatchOutboxProcessorTests()
    {
        _processor = new(_store, _workflowInstanceStore, _commandSender, NullLogger<WorkflowDispatchOutboxProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotDispatch_WhenOwnerWorkflowHasNotCommittedOutboxMarker()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: false)));

        await _processor.ProcessAsync();

        await _commandSender.DidNotReceiveWithAnyArgs().SendAsync(default(DispatchWorkflowDefinitionCommand)!, default!, default!, default);
        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DispatchesAndDeletes_WhenOwnerWorkflowCommittedOutboxMarker()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));

        await _processor.ProcessAsync();

        await _commandSender.Received(1).SendAsync(
            item.WorkflowDefinitionCommand!,
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_KeepsOutboxItem_WhenDispatchFails()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _commandSender
            .SendAsync(item.WorkflowDefinitionCommand!, CommandStrategy.Background, Arg.Any<IDictionary<object, object>>(), CancellationToken.None)
            .Returns(Task.FromException<Elsa.Mediator.Models.Unit>(new InvalidOperationException("Queue unavailable.")));

        await _processor.ProcessAsync();

        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    private static WorkflowDispatchOutboxItem CreateItem()
    {
        return new()
        {
            Id = "outbox-1",
            OwnerWorkflowInstanceId = "parent-1",
            Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
            WorkflowDefinitionCommand = new DispatchWorkflowDefinitionCommand("definition-version-1")
            {
                InstanceId = "child-1"
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static WorkflowInstance CreateOwnerWorkflowInstance(bool includeOutboxMarker)
    {
        var workflowState = new WorkflowState
        {
            Id = "parent-1",
            DefinitionId = "parent-definition",
            DefinitionVersionId = "parent-definition-version",
            Properties = includeOutboxMarker
                ? new Dictionary<string, object>
                {
                    [WorkflowDispatchOutboxStateExtensions.PropertyKey] = new WorkflowDispatchOutboxState
                    {
                        ItemIds = ["outbox-1"]
                    }
                }
                : new Dictionary<string, object>()
        };

        return new()
        {
            Id = workflowState.Id,
            DefinitionId = workflowState.DefinitionId,
            DefinitionVersionId = workflowState.DefinitionVersionId,
            WorkflowState = workflowState
        };
    }
}

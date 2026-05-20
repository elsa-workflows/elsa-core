using Elsa.Common;
using Elsa.Common.DistributedHosting;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowDispatchOutboxProcessorTests
{
    private readonly IWorkflowDispatchOutboxStore _store = Substitute.For<IWorkflowDispatchOutboxStore>();
    private readonly IWorkflowInstanceStore _workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
    private readonly ICommandSender _commandSender = Substitute.For<ICommandSender>();
    private readonly ISystemClock _systemClock = Substitute.For<ISystemClock>();
    private readonly WorkflowDispatcherOptions _options = new();
    private readonly WorkflowDispatchOutboxProcessor _processor;

    public WorkflowDispatchOutboxProcessorTests()
    {
        _systemClock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _processor = new(
            _store,
            _workflowInstanceStore,
            _commandSender,
            new NoopDistributedSynchronizationProvider(),
            _systemClock,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            Microsoft.Extensions.Options.Options.Create(_options),
            NullLogger<WorkflowDispatchOutboxProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotDispatch_WhenOwnerWorkflowHasNotCommittedOutboxMarker()
    {
        var item = CreateItem();
        _systemClock.UtcNow.Returns(item.CreatedAt.Add(_options.OrphanedOutboxItemRetention).AddTicks(-1));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: false)));

        await _processor.ProcessAsync();

        await _commandSender.DidNotReceiveWithAnyArgs().SendAsync(default(DispatchWorkflowDefinitionCommand)!, default!, default!, default);
        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeletesOutboxItem_WhenOwnerWorkflowNeverCommittedMarkerAfterRetentionPeriod()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: false)));

        await _processor.ProcessAsync();

        await _commandSender.DidNotReceiveWithAnyArgs().SendAsync(default(DispatchWorkflowDefinitionCommand)!, default!, default!, default);
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_KeepsOutboxItem_WhenOwnerWorkflowIsMissingWithinRetentionPeriod()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention).AddTicks(-1));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>((WorkflowInstance?)null));

        await _processor.ProcessAsync();

        await _commandSender.DidNotReceiveWithAnyArgs().SendAsync(default(DispatchWorkflowDefinitionCommand)!, default!, default!, default);
        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeletesOutboxItem_WhenOwnerWorkflowIsMissingAfterRetentionPeriod()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>((WorkflowInstance?)null));

        await _processor.ProcessAsync();

        await _commandSender.DidNotReceiveWithAnyArgs().SendAsync(default(DispatchWorkflowDefinitionCommand)!, default!, default!, default);
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DispatchesAndDeletes_WhenOwnerWorkflowCommittedOutboxMarker()
    {
        var item = CreateItem();
        var owner = CreateOwnerWorkflowInstance(includeOutboxMarker: true);
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(owner));

        await _processor.ProcessAsync();

        await _commandSender.Received(1).SendAsync(
            item.WorkflowDefinitionCommand!,
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        await _workflowInstanceStore.Received(1).SaveAsync(
            Arg.Is<WorkflowInstance>(x => !x.WorkflowState.HasWorkflowDispatchOutboxItem(item.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DoesNotRecreateOutboxItem_WhenMarkerCleanupFails()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _workflowInstanceStore.SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask>(_ => throw new InvalidOperationException("Store unavailable."));

        await _processor.ProcessAsync();

        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_KeepsOutboxItem_WhenDispatchFails()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _commandSender
            .SendAsync(item.WorkflowDefinitionCommand!, CommandStrategy.Background, Arg.Any<IDictionary<object, object>>(), CancellationToken.None)
            .Returns(Task.FromException<Elsa.Mediator.Models.Unit>(new InvalidOperationException("Queue unavailable.")));

        await _processor.ProcessAsync();

        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        await _store.Received(1).SaveAsync(Arg.Is<WorkflowDispatchOutboxItem>(x => x.Id == item.Id && x.DeliveryAttempts == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeletesOutboxItem_WhenMaxDeliveryAttemptsIsReached()
    {
        var item = CreateItem();
        item.DeliveryAttempts = _options.MaxOutboxDeliveryAttempts - 1;
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _commandSender
            .SendAsync(item.WorkflowDefinitionCommand!, CommandStrategy.Background, Arg.Any<IDictionary<object, object>>(), CancellationToken.None)
            .Returns(Task.FromException<Elsa.Mediator.Models.Unit>(new InvalidOperationException("Queue unavailable.")));

        await _processor.ProcessAsync();

        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    private static WorkflowDispatchOutboxItem CreateItem(DateTimeOffset? createdAt = null)
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
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
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

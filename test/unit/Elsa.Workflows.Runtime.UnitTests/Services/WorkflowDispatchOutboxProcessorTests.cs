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
using Medallion.Threading;
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
        _processor = CreateProcessor();
    }

    [Fact]
    public async Task ProcessAsync_ReturnsWithoutScanning_WhenProcessorLockCannotBeAcquired()
    {
        var processor = CreateProcessor(new ContendedDistributedSynchronizationProvider());

        await processor.ProcessAsync();

        await _store.DidNotReceiveWithAnyArgs().FindManyAsync(default, default);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsWithoutScanning_WhenProcessorLockAcquisitionTimesOut()
    {
        var processor = CreateProcessor(new TimeoutDistributedSynchronizationProvider());

        await processor.ProcessAsync();

        await _store.DidNotReceiveWithAnyArgs().FindManyAsync(default, default);
    }

    private WorkflowDispatchOutboxProcessor CreateProcessor(IDistributedLockProvider? distributedLockProvider = null)
    {
        return new(
            _store,
            _workflowInstanceStore,
            _commandSender,
            distributedLockProvider ?? new NoopDistributedSynchronizationProvider(),
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
    public async Task ProcessAsync_DoesNotCountDeliveryFailure_WhenUncommittedCleanupDeleteFails()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: false)));
        _store.DeleteAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        Assert.Equal(0, item.DeliveryAttempts);
        await _store.DidNotReceive().SaveAsync(item, Arg.Any<CancellationToken>());
        await _commandSender.DidNotReceiveWithAnyArgs().SendAsync(default(DispatchWorkflowDefinitionCommand)!, default!, default!, default);
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
    public async Task ProcessAsync_DoesNotCountDeliveryFailure_WhenMissingOwnerCleanupDeleteFails()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>((WorkflowInstance?)null));
        _store.DeleteAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        Assert.Equal(0, item.DeliveryAttempts);
        await _store.DidNotReceive().SaveAsync(item, Arg.Any<CancellationToken>());
        await _commandSender.DidNotReceiveWithAnyArgs().SendAsync(default(DispatchWorkflowDefinitionCommand)!, default!, default!, default);
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
    public async Task ProcessAsync_ReloadsOwnerBeforeRemovingCommittedMarker()
    {
        var item = CreateItem();
        var staleOwner = CreateOwnerWorkflowInstance(includeOutboxMarker: true);
        var freshOwner = CreateOwnerWorkflowInstance(includeOutboxMarker: true);
        freshOwner.Name = "fresh owner";
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<WorkflowInstance?>(staleOwner),
                new ValueTask<WorkflowInstance?>(freshOwner));

        await _processor.ProcessAsync();

        await _workflowInstanceStore.Received(1).SaveAsync(
            Arg.Is<WorkflowInstance>(x => ReferenceEquals(x, freshOwner) && !x.WorkflowState.HasWorkflowDispatchOutboxItem(item.Id)),
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
    public async Task ProcessAsync_DoesNotCountDeliveryFailure_WhenDeleteAfterSendFails()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _store.DeleteAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        await _commandSender.Received(1).SendAsync(
            item.WorkflowDefinitionCommand!,
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
        Assert.Equal(0, item.DeliveryAttempts);
        await _store.DidNotReceive().SaveAsync(item, Arg.Any<CancellationToken>());
        await _workflowInstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
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
    public async Task ProcessAsync_ContinuesBatch_WhenFailurePersistenceFails()
    {
        var failedItem = CreateItem(id: "outbox-1");
        var nextItem = CreateItem(id: "outbox-2");
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([failedItem, nextItem]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [failedItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])));
        _commandSender
            .SendAsync(failedItem.WorkflowDefinitionCommand!, CommandStrategy.Background, Arg.Any<IDictionary<object, object>>(), CancellationToken.None)
            .Returns(Task.FromException<Elsa.Mediator.Models.Unit>(new InvalidOperationException("Queue unavailable.")));
        _store.SaveAsync(failedItem, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Save failed.")));

        await _processor.ProcessAsync();

        await _commandSender.Received(1).SendAsync(
            nextItem.WorkflowDefinitionCommand!,
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
        await _store.Received(1).DeleteAsync(nextItem.Id, Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task ProcessAsync_ContinuesBatch_WhenMaxAttemptDeleteFails()
    {
        var failedItem = CreateItem(id: "outbox-1");
        var nextItem = CreateItem(id: "outbox-2");
        failedItem.DeliveryAttempts = _options.MaxOutboxDeliveryAttempts - 1;
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([failedItem, nextItem]);
        _workflowInstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [failedItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])));
        _commandSender
            .SendAsync(failedItem.WorkflowDefinitionCommand!, CommandStrategy.Background, Arg.Any<IDictionary<object, object>>(), CancellationToken.None)
            .Returns(Task.FromException<Elsa.Mediator.Models.Unit>(new InvalidOperationException("Queue unavailable.")));
        _store.DeleteAsync(failedItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        await _commandSender.Received(1).SendAsync(
            nextItem.WorkflowDefinitionCommand!,
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
        await _store.Received(1).SaveAsync(Arg.Is<WorkflowDispatchOutboxItem>(x => x.Id == failedItem.Id && x.DeliveryAttempts == _options.MaxOutboxDeliveryAttempts), Arg.Any<CancellationToken>());
        await _store.Received(1).DeleteAsync(nextItem.Id, Arg.Any<CancellationToken>());
    }

    private static WorkflowDispatchOutboxItem CreateItem(DateTimeOffset? createdAt = null, string id = "outbox-1")
    {
        return new()
        {
            Id = id,
            OwnerWorkflowInstanceId = "parent-1",
            Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
            WorkflowDefinitionCommand = new DispatchWorkflowDefinitionCommand("definition-version-1")
            {
                InstanceId = $"child-{id}"
            },
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };
    }

    private static WorkflowInstance CreateOwnerWorkflowInstance(bool includeOutboxMarker, ICollection<string>? itemIds = null)
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
                        ItemIds = itemIds ?? ["outbox-1"]
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

    private class ContendedDistributedSynchronizationProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new ContendedDistributedLock(name);
    }

    private class TimeoutDistributedSynchronizationProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new TimeoutDistributedLock(name);
    }

    private class ContendedDistributedLock(string name) : IDistributedLock
    {
        public string Name => name;

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) => null;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new TimeoutException();

        public virtual ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            return new ValueTask<IDistributedSynchronizationHandle?>((IDistributedSynchronizationHandle?)null);
        }

        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<IDistributedSynchronizationHandle>(new TimeoutException());
        }
    }

    private class TimeoutDistributedLock(string name) : ContendedDistributedLock(name)
    {
        public override ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<IDistributedSynchronizationHandle?>(new TimeoutException());
        }
    }
}

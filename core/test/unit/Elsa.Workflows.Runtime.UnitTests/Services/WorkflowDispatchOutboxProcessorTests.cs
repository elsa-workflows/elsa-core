using Elsa.Common;
using Elsa.Common.DistributedHosting;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Elsa.Workflows.State;
using Medallion.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowDispatchOutboxProcessorTests
{
    private readonly IWorkflowDispatchOutboxStore _store = Substitute.For<IWorkflowDispatchOutboxStore>();
    private readonly IWorkflowInstanceStore _workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
    private readonly IWorkflowDispatcher _innerDispatcher = Substitute.For<IWorkflowDispatcher>();
    private readonly ISystemClock _systemClock = Substitute.For<ISystemClock>();
    private readonly WorkflowDispatcherOptions _options = new();
    private readonly WorkflowDispatchOutboxProcessor _processor;

    public WorkflowDispatchOutboxProcessorTests()
    {
        _systemClock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _innerDispatcher.DispatchAsync(Arg.Any<DispatchWorkflowDefinitionRequest>(), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.Success());
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

    [Fact]
    public async Task TryProcessAsync_ReturnsFalseWithoutScanning_WhenProcessorLockAcquisitionTimesOut()
    {
        var processor = CreateProcessor(new TimeoutDistributedSynchronizationProvider());

        var result = await processor.TryProcessAsync();

        Assert.False(result);
        await _store.DidNotReceiveWithAnyArgs().FindManyAsync(default, default);
    }

    [Fact]
    public async Task ProcessAsync_UsesTenantScopedProcessorLock_WhenTenantContextIsAvailable()
    {
        var lockProvider = new RecordingDistributedSynchronizationProvider();
        var tenantAccessor = new DefaultTenantAccessor();
        var processor = CreateProcessor(lockProvider, tenantAccessor);
        using var tenantScope = tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Assert.Equal("Elsa:WorkflowDispatchOutbox:Processor:tenant-a", lockProvider.CurrentLockName);
                return [];
            });

        await processor.ProcessAsync();

        await _store.Received(1).FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryProcessAsync_UsesTenantScopedProcessorLock_WhenTenantContextIsAvailable()
    {
        var lockProvider = new RecordingDistributedSynchronizationProvider();
        var tenantAccessor = new DefaultTenantAccessor();
        var processor = CreateProcessor(lockProvider, tenantAccessor);
        using var tenantScope = tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Assert.Equal("Elsa:WorkflowDispatchOutbox:Processor:tenant-a", lockProvider.CurrentLockName);
                return [];
            });

        var result = await processor.TryProcessAsync();

        Assert.True(result);
        await _store.Received(1).FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private WorkflowDispatchOutboxProcessor CreateProcessor(IDistributedLockProvider? distributedLockProvider = null, ITenantAccessor? tenantAccessor = null, ITenantFinder? tenantFinder = null)
    {
        return new(
            _store,
            _workflowInstanceStore,
            _innerDispatcher,
            distributedLockProvider ?? new NoopDistributedSynchronizationProvider(),
            _systemClock,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            Microsoft.Extensions.Options.Options.Create(_options),
            NullLogger<WorkflowDispatchOutboxProcessor>.Instance,
            tenantAccessor,
            tenantFinder);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotDispatch_WhenOwnerWorkflowHasNotCommittedOutboxMarker()
    {
        var item = CreateItem();
        _systemClock.UtcNow.Returns(item.CreatedAt.Add(_options.OrphanedOutboxItemRetention).AddTicks(-1));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: false)));

        await _processor.ProcessAsync();

        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeletesOutboxItem_WhenOwnerWorkflowNeverCommittedMarkerAfterRetentionPeriod()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: false)));

        await _processor.ProcessAsync();

        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DoesNotCountDeliveryFailure_WhenUncommittedCleanupDeleteFails()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: false)));
        _store.DeleteAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        Assert.Equal(0, item.DeliveryAttempts);
        await _store.DidNotReceive().SaveAsync(item, Arg.Any<CancellationToken>());
        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
    }

    [Fact]
    public async Task ProcessAsync_KeepsOutboxItem_WhenOwnerWorkflowIsMissingWithinRetentionPeriod()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention).AddTicks(-1));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>((WorkflowInstance?)null));

        await _processor.ProcessAsync();

        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeletesOutboxItem_WhenOwnerWorkflowIsMissingAfterRetentionPeriod()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>((WorkflowInstance?)null));

        await _processor.ProcessAsync();

        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DoesNotCountDeliveryFailure_WhenMissingOwnerCleanupDeleteFails()
    {
        var createdAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var item = CreateItem(createdAt);
        _systemClock.UtcNow.Returns(createdAt.Add(_options.OrphanedOutboxItemRetention));
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>((WorkflowInstance?)null));
        _store.DeleteAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        Assert.Equal(0, item.DeliveryAttempts);
        await _store.DidNotReceive().SaveAsync(item, Arg.Any<CancellationToken>());
        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
    }

    [Fact]
    public async Task ProcessAsync_DispatchesAndDeletes_WhenOwnerWorkflowCommittedOutboxMarker()
    {
        var item = CreateItem();
        var owner = CreateOwnerWorkflowInstance(includeOutboxMarker: true);
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(owner));

        await _processor.ProcessAsync();

        await _innerDispatcher.Received(1).DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>());
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
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<WorkflowInstance?>(staleOwner),
                new ValueTask<WorkflowInstance?>(freshOwner));

        await _processor.ProcessAsync();

        await _workflowInstanceStore.Received(1).SaveAsync(
            Arg.Is<WorkflowInstance>(x => ReferenceEquals(x, freshOwner) && !x.WorkflowState.HasWorkflowDispatchOutboxItem(item.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_SavesMarkerRemovalWhileHoldingOwnerWorkflowInstanceLock()
    {
        var lockProvider = new RecordingDistributedSynchronizationProvider();
        var processor = CreateProcessor(lockProvider);
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _workflowInstanceStore.SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Assert.Equal("workflow-instance:parent-1", lockProvider.CurrentLockName);
                return ValueTask.CompletedTask;
            });

        await processor.ProcessAsync();

        await _workflowInstanceStore.Received(1).SaveAsync(
            Arg.Is<WorkflowInstance>(x => !x.WorkflowState.HasWorkflowDispatchOutboxItem(item.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DoesNotRecreateOutboxItem_WhenMarkerCleanupFails()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
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
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _store.DeleteAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        await _innerDispatcher.Received(1).DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>());
        Assert.Equal(0, item.DeliveryAttempts);
        await _store.DidNotReceive().SaveAsync(item, Arg.Any<CancellationToken>());
        await _workflowInstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_KeepsOutboxItem_WhenDispatchFails()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _innerDispatcher
            .DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DispatchWorkflowResponse>(new InvalidOperationException("Queue unavailable.")));

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
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [failedItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])));
        _innerDispatcher
            .DispatchAsync(DefinitionRequestFor(failedItem), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DispatchWorkflowResponse>(new InvalidOperationException("Queue unavailable.")));
        _store.SaveAsync(failedItem, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Save failed.")));

        await _processor.ProcessAsync();

        await _innerDispatcher.Received(1).DispatchAsync(DefinitionRequestFor(nextItem), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>());
        await _store.Received(1).DeleteAsync(nextItem.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeletesOutboxItem_WhenMaxDeliveryAttemptsIsReached()
    {
        var item = CreateItem();
        item.DeliveryAttempts = _options.MaxOutboxDeliveryAttempts - 1;
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _innerDispatcher
            .DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DispatchWorkflowResponse>(new InvalidOperationException("Queue unavailable.")));

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
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [failedItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])),
                new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, itemIds: [nextItem.Id])));
        _innerDispatcher
            .DispatchAsync(DefinitionRequestFor(failedItem), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DispatchWorkflowResponse>(new InvalidOperationException("Queue unavailable.")));
        _store.DeleteAsync(failedItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed.")));

        await _processor.ProcessAsync();

        await _innerDispatcher.Received(1).DispatchAsync(DefinitionRequestFor(nextItem), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveAsync(Arg.Is<WorkflowDispatchOutboxItem>(x => x.Id == failedItem.Id && x.DeliveryAttempts == _options.MaxOutboxDeliveryAttempts), Arg.Any<CancellationToken>());
        await _store.Received(1).DeleteAsync(nextItem.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeliversExactlyOnce_WhenFirstDeliveryThrowsAndRetrySucceeds()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item], [item], []);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _innerDispatcher
            .DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<DispatchWorkflowResponse>(new InvalidOperationException("Transport unavailable.")),
                Task.FromResult(DispatchWorkflowResponse.Success()));

        await _processor.ProcessAsync();

        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        await _store.Received(1).SaveAsync(Arg.Is<WorkflowDispatchOutboxItem>(x => x.Id == item.Id && x.DeliveryAttempts == 1), Arg.Any<CancellationToken>());

        await _processor.ProcessAsync();
        await _processor.ProcessAsync();

        await _innerDispatcher.Received(2).DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>());
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DoesNotCreateChildTwice_WhenRetryFindsChildAlreadyCreated()
    {
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _workflowInstanceStore.FindAsync(Arg.Is<WorkflowInstanceFilter>(x => x.Id == item.WorkflowDefinitionCommand!.InstanceId), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance { Id = item.WorkflowDefinitionCommand!.InstanceId! }));

        await _processor.ProcessAsync();

        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_RoutesThroughInnerDispatcherOnPersistedChannel()
    {
        _options.Channels.Add(new DispatcherChannel { Name = "priority" });
        var item = CreateItem();
        item.Channel = "priority";
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));

        await _processor.ProcessAsync();

        await _innerDispatcher.Received(1).DispatchAsync(
            DefinitionRequestFor(item),
            Arg.Is<DispatchWorkflowOptions?>(x => x != null && x.Channel == "priority"),
            Arg.Any<CancellationToken>());
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FailsClosed_WhenChannelCannotBeRouted()
    {
        var item = CreateItem();
        item.Channel = "missing";
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));

        await _processor.ProcessAsync();

        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        await _store.Received(1).SaveAsync(Arg.Is<WorkflowDispatchOutboxItem>(x => x.Id == item.Id && x.DeliveryAttempts == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FailsClosed_WhenInnerDispatcherRejectsChannel()
    {
        _options.Channels.Add(new DispatcherChannel { Name = "priority" });
        var item = CreateItem();
        item.Channel = "priority";
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _innerDispatcher.DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.UnknownChannel());

        await _processor.ProcessAsync();

        await _store.DidNotReceive().DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        await _store.Received(1).SaveAsync(Arg.Is<WorkflowDispatchOutboxItem>(x => x.Id == item.Id && x.DeliveryAttempts == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_RestoresItemTenantBeforeDispatching()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        var tenantFinder = Substitute.For<ITenantFinder>();
        tenantFinder.FindByIdAsync("tenant-b", Arg.Any<CancellationToken>()).Returns(new Tenant { Id = "tenant-b" });
        var processor = CreateProcessor(tenantAccessor: tenantAccessor, tenantFinder: tenantFinder);
        var item = CreateItem();
        item.TenantId = "tenant-b";
        string? dispatchTenantId = null;
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _innerDispatcher.DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                dispatchTenantId = tenantAccessor.Tenant?.Id;
                return DispatchWorkflowResponse.Success();
            });

        await processor.ProcessAsync();

        Assert.Equal("tenant-b", dispatchTenantId);
        Assert.Null(tenantAccessor.Tenant);
    }

    [Fact]
    public void OutboxItem_StoredBeforeChannelExisted_StillLoads()
    {
        const string legacyJson = """
            {"id":"outbox-1","ownerWorkflowInstanceId":"parent-1","tenantId":"tenant-a","kind":"WorkflowDefinition","createdAt":"2026-05-20T12:00:00+00:00","deliveryAttempts":2,"workflowDefinitionCommand":{"definitionVersionId":"definition-version-1","instanceId":"child-1","skipIfInstanceExists":true}}
            """;
        var serializer = CreatePayloadSerializer();

        var item = serializer.Deserialize<WorkflowDispatchOutboxItem>(legacyJson);

        Assert.Null(item.Channel);
        Assert.Equal(WorkflowDispatchOutboxItemKind.WorkflowDefinition, item.Kind);
        Assert.Equal("child-1", item.WorkflowDefinitionCommand!.InstanceId);
        Assert.Equal(2, item.DeliveryAttempts);
    }

    [Fact]
    public void OutboxItem_PersistsChannel()
    {
        var serializer = CreatePayloadSerializer();
        var item = CreateItem();
        item.Channel = "priority";

        var roundTripped = serializer.Deserialize<WorkflowDispatchOutboxItem>(serializer.Serialize(item));

        Assert.Equal("priority", roundTripped.Channel);
    }

    private static IPayloadSerializer CreatePayloadSerializer() =>
        new Elsa.Workflows.Serialization.Serializers.JsonPayloadSerializer(new ServiceCollection()
            .AddSingleton<Elsa.Common.Serialization.ISerializationTypeRegistry>(new Elsa.Common.Serialization.SerializationTypeRegistry(Microsoft.Extensions.Options.Options.Create(new Elsa.Common.Serialization.SerializationTypeOptions())))
            .AddLogging()
            .BuildServiceProvider());

    private static WorkflowInstanceFilter OwnerFilter() => Arg.Is<WorkflowInstanceFilter>(x => x.Id == "parent-1");

    private static DispatchWorkflowDefinitionRequest DefinitionRequestFor(WorkflowDispatchOutboxItem item) =>
        Arg.Is<DispatchWorkflowDefinitionRequest>(x => x.InstanceId == item.WorkflowDefinitionCommand!.InstanceId);

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

    private class RecordingDistributedSynchronizationProvider : IDistributedLockProvider
    {
        private readonly AsyncLocal<Stack<string>?> _heldLockNames = new();

        public string? CurrentLockName => _heldLockNames.Value is { Count: > 0 } heldLockNames ? heldLockNames.Peek() : null;

        public IDistributedLock CreateLock(string name) => new RecordingDistributedLock(this, name);

        private IDistributedSynchronizationHandle Acquire(string name)
        {
            var heldLockNames = _heldLockNames.Value ??= new Stack<string>();
            heldLockNames.Push(name);
            return new RecordingDistributedSynchronizationHandle(this, name);
        }

        private void Release(string name)
        {
            var heldLockNames = _heldLockNames.Value ?? throw new InvalidOperationException("No distributed locks are held.");
            var currentLockName = heldLockNames.Pop();

            if (currentLockName != name)
                throw new InvalidOperationException($"Expected to release distributed lock '{currentLockName}', but '{name}' was released.");

            if (heldLockNames.Count == 0)
                _heldLockNames.Value = null;
        }

        private class RecordingDistributedLock(RecordingDistributedSynchronizationProvider provider, string name) : IDistributedLock
        {
            public string Name => name;

            public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) => provider.Acquire(name);

            public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => provider.Acquire(name);

            public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
            {
                return new ValueTask<IDistributedSynchronizationHandle?>(provider.Acquire(name));
            }

            public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            {
                return new ValueTask<IDistributedSynchronizationHandle>(provider.Acquire(name));
            }
        }

        private class RecordingDistributedSynchronizationHandle(RecordingDistributedSynchronizationProvider provider, string name) : IDistributedSynchronizationHandle
        {
            private int _disposed;

            public CancellationToken HandleLostToken => CancellationToken.None;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    provider.Release(name);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
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

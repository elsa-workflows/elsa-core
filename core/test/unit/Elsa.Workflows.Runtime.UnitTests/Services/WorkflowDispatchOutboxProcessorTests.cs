using Microsoft.Extensions.Logging;
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

    private WorkflowDispatchOutboxProcessor CreateProcessor(IDistributedLockProvider? distributedLockProvider = null, ITenantAccessor? tenantAccessor = null, ITenantFinder? tenantFinder = null, IWorkflowDispatcher? innerDispatcher = null, ILogger<WorkflowDispatchOutboxProcessor>? logger = null)
    {
        return new(
            _store,
            _workflowInstanceStore,
            innerDispatcher ?? _innerDispatcher,
            distributedLockProvider ?? new NoopDistributedSynchronizationProvider(),
            _systemClock,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            Microsoft.Extensions.Options.Options.Create(_options),
            logger ?? NullLogger<WorkflowDispatchOutboxProcessor>.Instance,
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
    public async Task ProcessAsync_AsksInnerDispatcherToSkipExistingChild()
    {
        var item = CreateItem();
        item.WorkflowDefinitionCommand!.SkipIfInstanceExists = true;
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));

        await _processor.ProcessAsync();

        await _innerDispatcher.Received(1).DispatchAsync(
            Arg.Is<DispatchWorkflowDefinitionRequest>(x => x.InstanceId == "child-outbox-1" && x.SkipIfInstanceExists),
            Arg.Any<DispatchWorkflowOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void BackgroundDispatchCommand_CarriesSkipIfInstanceExistsFromRequest()
    {
        var command = WorkflowDispatchCommandFactory.CreateCommand(new DispatchWorkflowDefinitionRequest("definition-version-1")
        {
            InstanceId = "child-1",
            SkipIfInstanceExists = true
        });

        Assert.Equal("child-1", command.InstanceId);
        Assert.True(command.SkipIfInstanceExists);
    }

    [Fact]
    public async Task ProcessAsync_SendsDispatchNotificationsExactlyOnce_AtDelivery()
    {
        var notificationSender = Substitute.For<INotificationSender>();
        var background = new BackgroundWorkflowDispatcher(Substitute.For<ICommandSender>(), notificationSender, new DefaultTenantAccessor());
        var processor = CreateProcessor(innerDispatcher: background);
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));

        await processor.ProcessAsync();

        await notificationSender.Received(1).SendAsync(Arg.Any<Elsa.Workflows.Runtime.Notifications.WorkflowDefinitionDispatching>(), Arg.Any<CancellationToken>());
        await notificationSender.Received(1).SendAsync(Arg.Any<Elsa.Workflows.Runtime.Notifications.WorkflowDefinitionDispatched>(), Arg.Any<CancellationToken>());
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
    public async Task ProcessAsync_AbandonsItemAtMaxAttempts_WhenChannelIsNotConfigured()
    {
        var item = CreateItem();
        item.Channel = "missing";
        var logger = new CapturingLogger<WorkflowDispatchOutboxProcessor>();
        var processor = SetUpUnroutable(item, logger);

        await RunPastMaxAttemptsAsync(processor);

        AssertAbandoned(item, logger, "missing");
        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
    }

    [Fact]
    public async Task ProcessAsync_AbandonsItemAtMaxAttempts_WhenInnerDispatcherReturnsUnknownChannel()
    {
        _options.Channels.Add(new DispatcherChannel { Name = "priority" });
        var item = CreateItem();
        item.Channel = "priority";
        var logger = new CapturingLogger<WorkflowDispatchOutboxProcessor>();
        var processor = SetUpUnroutable(item, logger);
        _innerDispatcher.DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.UnknownChannel());

        await RunPastMaxAttemptsAsync(processor);

        AssertAbandoned(item, logger, "priority");
        await _innerDispatcher.Received(_options.MaxOutboxDeliveryAttempts).DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>());
    }

    private WorkflowDispatchOutboxProcessor SetUpUnroutable(WorkflowDispatchOutboxItem item, CapturingLogger<WorkflowDispatchOutboxProcessor> logger)
    {
        // The store keeps returning the item until it is deleted, like a real outbox.
        var deleted = false;
        _store.DeleteAsync(item.Id, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            deleted = true;
            return Task.CompletedTask;
        });
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(_ => deleted ? [] : new[] { item });
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        return CreateProcessor(logger: logger);
    }

    private async Task RunPastMaxAttemptsAsync(WorkflowDispatchOutboxProcessor processor)
    {
        for (var pass = 0; pass < _options.MaxOutboxDeliveryAttempts + 1; pass++)
        {
            await processor.ProcessAsync();
        }
    }

    private void AssertAbandoned(WorkflowDispatchOutboxItem item, CapturingLogger<WorkflowDispatchOutboxProcessor> logger, string channel)
    {
        // Unroutable passes count as failed attempts (#8816), so the item is abandoned at the limit instead of stalling the batch.
        _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        Assert.Equal(_options.MaxOutboxDeliveryAttempts, item.DeliveryAttempts);
        Assert.Empty(_store.FindManyAsync(1, CancellationToken.None).Result);
        Assert.Equal(_options.MaxOutboxDeliveryAttempts - 1, logger.Entries.Count(x => x.Level == LogLevel.Error && x.Message.Contains($"'{channel}'")));
        var warning = Assert.Single(logger.Entries, x => x.Level == LogLevel.Warning);
        Assert.Contains($"'{channel}'", warning.Message);
    }

    [Fact]
    public async Task ProcessAsync_DeliversNewerItem_WhenFullBatchOfRejectedItemsIsAhead()
    {
        // #8816: real processor and key-value store. A full batch of rejected items must not stall a newer deliverable item.
        _options.OutboxProcessorBatchSize = 5;
        _options.MaxOutboxDeliveryAttempts = 1;
        var store = new KeyValueWorkflowDispatchOutboxStore(new Elsa.KeyValues.Stores.MemoryKeyValueStore(new Elsa.Common.Services.MemoryStore<Elsa.KeyValues.Entities.SerializedKeyValuePair>()), CreatePayloadSerializer());
        var start = new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);
        var rejected = Enumerable.Range(0, _options.OutboxProcessorBatchSize).Select(i =>
        {
            var x = CreateItem(start.AddSeconds(i), $"rejected-{i}");
            x.Channel = "missing";
            return x;
        }).ToList();
        var deliverable = CreateItem(start.AddMinutes(1), "deliverable");
        foreach (var x in rejected.Append(deliverable))
        {
            await store.SaveAsync(x);
        }
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true, rejected.Append(deliverable).Select(x => x.Id).ToList())));
        var processor = new WorkflowDispatchOutboxProcessor(store, _workflowInstanceStore, _innerDispatcher, new NoopDistributedSynchronizationProvider(), _systemClock,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()), Microsoft.Extensions.Options.Options.Create(_options), NullLogger<WorkflowDispatchOutboxProcessor>.Instance);
        _innerDispatcher.DispatchAsync(Arg.Any<DispatchWorkflowDefinitionRequest>(), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.Success());

        var passes = 0;
        while (passes < 2 && !_innerDispatcher.ReceivedCalls().Any(c => c.GetArguments()[0] is DispatchWorkflowDefinitionRequest r && r.InstanceId == "child-deliverable"))
        {
            await processor.ProcessAsync();
            passes++;
        }

        await _innerDispatcher.Received(1).DispatchAsync(Arg.Is<DispatchWorkflowDefinitionRequest>(r => r.InstanceId == "child-deliverable"), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>());
        Assert.Empty(await store.FindManyAsync());
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

    [Fact]
    public async Task ObsoleteConstructor_DeliversThroughBackgroundCommands_NotTheRegisteredDispatcher()
    {
        var commandSender = Substitute.For<ICommandSender>();
#pragma warning disable CS0618
        var processor = new WorkflowDispatchOutboxProcessor(_store, _workflowInstanceStore, commandSender, new NoopDistributedSynchronizationProvider(), _systemClock,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()), Microsoft.Extensions.Options.Options.Create(_options), NullLogger<WorkflowDispatchOutboxProcessor>.Instance);
#pragma warning restore CS0618
        var item = CreateItem();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));

        await processor.ProcessAsync();

        await commandSender.Received(1).SendAsync(Arg.Is<DispatchWorkflowDefinitionCommand>(x => x.InstanceId == "child-outbox-1"), CommandStrategy.Background, Arg.Any<IDictionary<object, object>>(), Arg.Any<CancellationToken>());
        // The obsolete path takes no IWorkflowDispatcher: with the outbox on, the registered one would enqueue the item again.
        Assert.DoesNotContain(typeof(WorkflowDispatchOutboxProcessor).GetConstructors().Single(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), false).Any()).GetParameters(),
            x => x.ParameterType == typeof(IWorkflowDispatcher) || x.ParameterType == typeof(IServiceProvider));
        Assert.Empty(_innerDispatcher.ReceivedCalls());
        await _store.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_NamesChannel_WhenInnerDispatcherRejectsIt()
    {
        _options.Channels.Add(new DispatcherChannel { Name = "priority" });
        var item = CreateItem();
        item.Channel = "priority";
        var logger = new CapturingLogger<WorkflowDispatchOutboxProcessor>();
        _store.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        _workflowInstanceStore.FindAsync(OwnerFilter(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(CreateOwnerWorkflowInstance(includeOutboxMarker: true)));
        _innerDispatcher.DispatchAsync(DefinitionRequestFor(item), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.UnknownChannel());
        var processor = new WorkflowDispatchOutboxProcessor(_store, _workflowInstanceStore, _innerDispatcher, new NoopDistributedSynchronizationProvider(), _systemClock,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()), Microsoft.Extensions.Options.Options.Create(_options), logger);

        await processor.ProcessAsync();

        var entry = Assert.Single(logger.Entries, x => x.Level == LogLevel.Error);
        Assert.Contains("'priority'", entry.Message);
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

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }
}

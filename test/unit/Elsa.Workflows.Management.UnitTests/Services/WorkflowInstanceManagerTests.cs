using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowInstanceManagerTests
{
    [Fact]
    public async Task BulkDeleteAsync_Should_Delete_In_Batches_When_Count_Exceeds_BatchSize()
    {
        // Arrange
        var batchSize = 10;
        var totalInstances = 25;
        var store = Substitute.For<IWorkflowInstanceStore>();
        var notificationSender = Substitute.For<INotificationSender>();
        var options = Microsoft.Extensions.Options.Options.Create(new ManagementOptions { BulkDeleteBatchSize = batchSize });
        var logger = Substitute.For<ILogger<WorkflowInstanceManager>>();
        
        var manager = CreateManager(store, notificationSender, options, logger);
        
        // Setup store to return IDs in batches
        var allIds = Enumerable.Range(1, totalInstances).Select(i => $"id-{i}").ToList();
        
        // First call returns first batch
        store.FindManyIdsAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Is<PageArgs>(p => p.Limit == batchSize),
            Arg.Any<CancellationToken>())
            .Returns(
                Page.Of(allIds.Take(batchSize).ToList(), totalInstances),
                Page.Of(allIds.Skip(batchSize).Take(batchSize).ToList(), totalInstances - batchSize),
                Page.Of(allIds.Skip(batchSize * 2).Take(batchSize).ToList(), totalInstances - batchSize * 2),
                Page.Of(new List<string>(), 0) // Empty to signal completion
            );
        
        // Setup delete to return the number of items deleted
        store.DeleteAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var filter = callInfo.ArgAt<WorkflowInstanceFilter>(0);
                long count = filter.Ids?.Count() ?? 0;
                return ValueTask.FromResult(count);
            });
        
        var filter = new WorkflowInstanceFilter();
        
        // Act
        var result = await manager.BulkDeleteAsync(filter, CancellationToken.None);
        
        // Assert
        Assert.Equal(totalInstances, result);
        
        // Verify we made at least 3 calls to FindManyIdsAsync (for 3 batches)
        await store.Received(3).FindManyIdsAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Is<PageArgs>(p => p.Limit == batchSize),
            Arg.Any<CancellationToken>());
        
        // Verify we made 3 calls to DeleteAsync (for 3 batches)
        await store.Received(3).DeleteAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkDeleteAsync_Should_Delete_Single_Batch_When_Count_Less_Than_BatchSize()
    {
        // Arrange
        var batchSize = 100;
        var totalInstances = 50;
        var store = Substitute.For<IWorkflowInstanceStore>();
        var notificationSender = Substitute.For<INotificationSender>();
        var options = Microsoft.Extensions.Options.Options.Create(new ManagementOptions { BulkDeleteBatchSize = batchSize });
        var logger = Substitute.For<ILogger<WorkflowInstanceManager>>();
        
        var manager = CreateManager(store, notificationSender, options, logger);
        
        var allIds = Enumerable.Range(1, totalInstances).Select(i => $"id-{i}").ToList();
        
        store.FindManyIdsAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Any<PageArgs>(),
            Arg.Any<CancellationToken>())
            .Returns(Page.Of(allIds, totalInstances));
        
        store.DeleteAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult((long)totalInstances));
        
        var filter = new WorkflowInstanceFilter();
        
        // Act
        var result = await manager.BulkDeleteAsync(filter, CancellationToken.None);
        
        // Assert
        Assert.Equal(totalInstances, result);
        
        // Verify we made only 1 call to FindManyIdsAsync
        await store.Received(1).FindManyIdsAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Any<PageArgs>(),
            Arg.Any<CancellationToken>());
        
        // Verify we made only 1 call to DeleteAsync
        await store.Received(1).DeleteAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkDeleteAsync_Should_Return_Zero_When_No_Instances_Match()
    {
        // Arrange
        var store = Substitute.For<IWorkflowInstanceStore>();
        var notificationSender = Substitute.For<INotificationSender>();
        var options = Microsoft.Extensions.Options.Options.Create(new ManagementOptions { BulkDeleteBatchSize = 100 });
        var logger = Substitute.For<ILogger<WorkflowInstanceManager>>();
        
        var manager = CreateManager(store, notificationSender, options, logger);
        
        // Return empty result
        store.FindManyIdsAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Any<PageArgs>(),
            Arg.Any<CancellationToken>())
            .Returns(Page.Of(new List<string>(), 0));
        
        var filter = new WorkflowInstanceFilter();
        
        // Act
        var result = await manager.BulkDeleteAsync(filter, CancellationToken.None);
        
        // Assert
        Assert.Equal(0, result);
        
        // Verify we never called DeleteAsync since there were no instances
        await store.DidNotReceive().DeleteAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkDeleteAsync_Should_Send_Notifications_For_Each_Batch()
    {
        // Arrange
        var batchSize = 5;
        var totalInstances = 12;
        var store = Substitute.For<IWorkflowInstanceStore>();
        var notificationSender = Substitute.For<INotificationSender>();
        var options = Microsoft.Extensions.Options.Options.Create(new ManagementOptions { BulkDeleteBatchSize = batchSize });
        var logger = Substitute.For<ILogger<WorkflowInstanceManager>>();
        
        var manager = CreateManager(store, notificationSender, options, logger);
        
        var allIds = Enumerable.Range(1, totalInstances).Select(i => $"id-{i}").ToList();
        
        store.FindManyIdsAsync(
            Arg.Any<WorkflowInstanceFilter>(), 
            Arg.Is<PageArgs>(p => p.Limit == batchSize),
            Arg.Any<CancellationToken>())
            .Returns(
                Page.Of(allIds.Take(batchSize).ToList(), totalInstances),
                Page.Of(allIds.Skip(batchSize).Take(batchSize).ToList(), totalInstances - batchSize),
                Page.Of(allIds.Skip(batchSize * 2).Take(batchSize).ToList(), totalInstances - batchSize * 2),
                Page.Of(new List<string>(), 0)
            );
        
        store.DeleteAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var filter = callInfo.ArgAt<WorkflowInstanceFilter>(0);
                long count = filter.Ids?.Count() ?? 0;
                return ValueTask.FromResult(count);
            });
        
        var filter = new WorkflowInstanceFilter();
        
        // Act
        await manager.BulkDeleteAsync(filter, CancellationToken.None);
        
        // Assert - notifications should be sent for each batch (before and after)
        // 3 batches = 6 notifications total
        await notificationSender.Received(6).SendAsync(
            Arg.Any<INotification>(), 
            Arg.Any<CancellationToken>());
    }

    private static WorkflowInstanceManager CreateManager(
        IWorkflowInstanceStore store,
        INotificationSender notificationSender,
        IOptions<ManagementOptions> options,
        ILogger<WorkflowInstanceManager> logger)
    {
        var factory = Substitute.For<IWorkflowInstanceFactory>();
        var stateMapper = new WorkflowStateMapper();
        var stateExtractor = Substitute.For<IWorkflowStateExtractor>();
        var stateSerializer = Substitute.For<IWorkflowStateSerializer>();
        
        return new WorkflowInstanceManager(
            store,
            factory,
            notificationSender,
            stateMapper,
            stateExtractor,
            stateSerializer,
            options,
            logger);
    }
}

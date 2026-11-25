using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BookmarkQueueDelivery;

public class Tests
{
    [Fact]
    public async Task EarlyStimulus_WakesOnceAfterBookmarkCommit_AndDelivers()
    {
        const string instanceId = "delivery-instance";
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        await using var provider = await BuildServicesAsync(signaler);
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var queue = services.GetRequiredService<IBookmarkQueue>();
        var queueStore = services.GetRequiredService<IBookmarkQueueStore>();
        var processor = services.GetRequiredService<IBookmarkQueueProcessor>();
        var instanceStore = services.GetRequiredService<IWorkflowInstanceStore>();
        var instanceFilter = new WorkflowInstanceFilter { Id = instanceId };

        await queue.EnqueueAsync(new() { WorkflowInstanceId = instanceId });
        await signaler.Received(1).TriggerAsync(Arg.Any<CancellationToken>());
        await processor.ProcessAsync();
        Assert.Single(await queueStore.FindManyAsync(new BookmarkQueueFilter()));
        var bookmarkCountsAtWakeup = new List<int>();
        signaler.TriggerAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            var instance = await instanceStore.FindAsync(instanceFilter);
            bookmarkCountsAtWakeup.Add(instance?.WorkflowState.Bookmarks.Count ?? 0);
        });

        var client = await services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(instanceId);
        await client.CreateAndRunInstanceAsync(new() { WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("queue-delivery") });
        Assert.Equal([1], bookmarkCountsAtWakeup);

        await processor.ProcessAsync();

        Assert.Empty(await queueStore.FindManyAsync(new BookmarkQueueFilter()));
        var completed = await instanceStore.FindAsync(instanceFilter);
        Assert.Equal(WorkflowStatus.Finished, completed?.WorkflowState.Status);
        Assert.Equal([1], bookmarkCountsAtWakeup);
    }

    [Fact]
    public async Task BookmarkFreeWorkflow_DoesNotWakeQueue()
    {
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        await using var provider = await BuildServicesAsync(signaler);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IWorkflowRunner>().RunAsync(new Sequence());

        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        await signaler.DidNotReceive().TriggerAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExplicitBookmarkSave_WakesQueueWithCallerCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        await using var provider = await BuildServicesAsync(signaler);
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IBookmarkManager>().SaveAsync(new StoredBookmark { Id = "saved" }, cancellation.Token);

        await signaler.Received(1).TriggerAsync(cancellation.Token);
    }

    private static async Task<ServiceProvider> BuildServicesAsync(IBookmarkQueueSignaler signaler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddElsa(elsa => elsa
            .UseWorkflowManagement()
            .UseWorkflowRuntime(runtime => runtime.DistributedLockProvider = _ => new NoopDistributedSynchronizationProvider())
            .AddWorkflow<DeliveryWorkflow>());
        services.AddSingleton(signaler);
        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.PopulateRegistriesAsync();
        return provider;
    }

    public class DeliveryWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId("queue-delivery");
            builder.Root = new Event("deliver");
        }
    }
}

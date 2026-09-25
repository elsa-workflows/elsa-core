using Elsa.Caching.Distributed.ProtoActor.IntegrationTests.Infrastructure;
using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Proto.Cluster.PubSub;
using Proto.Cluster.Testing;

namespace Elsa.Caching.Distributed.ProtoActor.IntegrationTests.Scenarios;

public class StartLocalCacheActorTests
{
    private const string ActorName = "$memory-cache-invalidator";
    private const string Topic = "change-token-signals";

    [Fact]
    public async Task StartAsync_WhenSubscriptionPersistenceIsBlocked_DoesNotComplete()
    {
        var subscribersStore = new TestSubscribersStore();
        subscribersStore.PauseNextWrite();
        var agent = new InMemAgent();
        await using var node = ProtoActorCacheNode.Create(NewClusterName(), agent, subscribersStore);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var startTask = node.StartAsync(cancellationTokenSource.Token);

        try
        {
            await subscribersStore.WaitForPausedWriteAsync(cancellationTokenSource.Token);
            Assert.False(startTask.IsCompleted);
            Assert.Empty((await subscribersStore.GetAsync(Topic, cancellationTokenSource.Token)).Subscribers_);
        }
        finally
        {
            subscribersStore.ResumePausedWrite();
            await startTask;
        }

        var subscriber = Assert.Single((await subscribersStore.GetAsync(Topic, cancellationTokenSource.Token)).Subscribers_);
        Assert.Equal(SubscriberIdentity.IdentityOneofCase.Pid, subscriber.IdentityCase);
        Assert.Equal(ActorName, subscriber.Pid.Id);
        Assert.Equal(node.ActorSystem.Address, subscriber.Pid.Address);
        Assert.Equal(subscriber.Pid, Assert.Single(node.ActorSystem.ProcessRegistry.Find(id => id == ActorName)));
    }

    [Fact]
    public async Task StartAsync_WhenSubscriptionRequestIsCancelled_StopsSpawnedActorAndPropagatesException()
    {
        var subscribersStore = new TestSubscribersStore();
        subscribersStore.PauseNextWrite();
        var agent = new InMemAgent();
        await using var node = ProtoActorCacheNode.Create(NewClusterName(), agent, subscribersStore);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var startTask = node.StartAsync(cancellationTokenSource.Token);

        try
        {
            await subscribersStore.WaitForPausedWriteAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<TimeoutException>(() => startTask);
            Assert.Empty(node.ActorSystem.ProcessRegistry.Find(id => id == ActorName));
        }
        finally
        {
            subscribersStore.ResumePausedWrite();
        }
    }

    [Fact]
    public async Task StartAsync_WhenTwoMembersJoin_RegistersOneLocalPidPerMember()
    {
        var subscribersStore = new TestSubscribersStore();
        var agent = new InMemAgent();
        var clusterName = NewClusterName();
        await using var firstNode = ProtoActorCacheNode.Create(clusterName, agent, subscribersStore);
        await using var secondNode = ProtoActorCacheNode.Create(clusterName, agent, subscribersStore);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var cancellationToken = cancellationTokenSource.Token;

        await firstNode.StartAsync(cancellationToken);
        await secondNode.StartAsync(cancellationToken);

        var subscribers = await AssertPidSubscribersAsync(subscribersStore, firstNode, secondNode, cancellationToken);
        Assert.Contains(Assert.Single(firstNode.ActorSystem.ProcessRegistry.Find(id => id == ActorName)), subscribers.Select(x => x.Pid));
        Assert.Contains(Assert.Single(secondNode.ActorSystem.ProcessRegistry.Find(id => id == ActorName)), subscribers.Select(x => x.Pid));
    }

    [Fact]
    public async Task PublishBatch_WhenTwoMembersAreSubscribed_DeliversExactlyOncePerMember()
    {
        var subscribersStore = new TestSubscribersStore();
        var agent = new InMemAgent();
        var clusterName = NewClusterName();
        await using var firstNode = ProtoActorCacheNode.Create(clusterName, agent, subscribersStore);
        await using var secondNode = ProtoActorCacheNode.Create(clusterName, agent, subscribersStore);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var cancellationToken = cancellationTokenSource.Token;

        await firstNode.StartAsync(cancellationToken);
        await secondNode.StartAsync(cancellationToken);
        await AssertPidSubscribersAsync(subscribersStore, firstNode, secondNode, cancellationToken);

        var signalKey = $"signal-{Guid.NewGuid():N}";
        var fenceKey = $"fence-{Guid.NewGuid():N}";
        var firstFence = firstNode.SignalInvoker.WaitForSignalAsync(fenceKey, cancellationToken);
        var secondFence = secondNode.SignalInvoker.WaitForSignalAsync(fenceKey, cancellationToken);

        var publishResponse = await PublishSignalAndFenceAsync(firstNode, signalKey, fenceKey, cancellationToken);

        Assert.Equal(PublishStatus.Ok, publishResponse.Status);
        await Task.WhenAll(firstFence, secondFence);
        Assert.Equal(1, firstNode.SignalInvoker.GetCount(signalKey));
        Assert.Equal(1, secondNode.SignalInvoker.GetCount(signalKey));
        Assert.Equal(1, firstNode.SignalInvoker.GetCount(fenceKey));
        Assert.Equal(1, secondNode.SignalInvoker.GetCount(fenceKey));
    }

    [Fact]
    public async Task StopAsync_WhenOneMemberStops_UnsubscribesItsPidAndStopsItsActor()
    {
        var subscribersStore = new TestSubscribersStore();
        var agent = new InMemAgent();
        var clusterName = NewClusterName();
        await using var firstNode = ProtoActorCacheNode.Create(clusterName, agent, subscribersStore);
        await using var secondNode = ProtoActorCacheNode.Create(clusterName, agent, subscribersStore);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var cancellationToken = cancellationTokenSource.Token;

        await firstNode.StartAsync(cancellationToken);
        await secondNode.StartAsync(cancellationToken);
        var subscribers = await AssertPidSubscribersAsync(subscribersStore, firstNode, secondNode, cancellationToken);

        var firstPid = subscribers.Single(subscriber => subscriber.Pid.Address == firstNode.ActorSystem.Address).Pid;
        await secondNode.StopAsync(cancellationToken);

        var remainingSubscriber = Assert.Single((await subscribersStore.GetAsync(Topic, cancellationToken)).Subscribers_);
        Assert.Equal(SubscriberIdentity.IdentityOneofCase.Pid, remainingSubscriber.IdentityCase);
        Assert.Equal(firstPid, remainingSubscriber.Pid);
        Assert.Equal(firstPid, Assert.Single(firstNode.ActorSystem.ProcessRegistry.Find(id => id == ActorName)));
        Assert.Empty(secondNode.ActorSystem.ProcessRegistry.Find(id => id == ActorName));

        var postStopSignalKey = $"post-stop-signal-{Guid.NewGuid():N}";
        var postStopFenceKey = $"post-stop-fence-{Guid.NewGuid():N}";
        var postStopFence = firstNode.SignalInvoker.WaitForSignalAsync(postStopFenceKey, cancellationToken);

        var publishResponse = await PublishSignalAndFenceAsync(firstNode, postStopSignalKey, postStopFenceKey, cancellationToken);

        Assert.Equal(PublishStatus.Ok, publishResponse.Status);
        await postStopFence;
        Assert.Equal(1, firstNode.SignalInvoker.GetCount(postStopSignalKey));
        Assert.Equal(0, secondNode.SignalInvoker.GetCount(postStopSignalKey));
    }

    [Fact]
    public async Task StopAsync_WhenUnsubscriptionRequestIsCancelled_StopsActorAndPropagatesException()
    {
        var subscribersStore = new TestSubscribersStore();
        var agent = new InMemAgent();
        await using var node = ProtoActorCacheNode.Create(NewClusterName(), agent, subscribersStore);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = cancellationTokenSource.Token;

        await node.StartAsync(cancellationToken);
        subscribersStore.PauseNextWrite();
        var stopTask = node.StopAsync(cancellationToken);

        try
        {
            await subscribersStore.WaitForPausedWriteAsync(cancellationToken);
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<TimeoutException>(() => stopTask);
            Assert.Empty(node.ActorSystem.ProcessRegistry.Find(id => id == ActorName));
        }
        finally
        {
            subscribersStore.ResumePausedWrite();
        }
    }

    private static async Task<SubscriberIdentity[]> AssertPidSubscribersAsync(
        TestSubscribersStore subscribersStore,
        ProtoActorCacheNode firstNode,
        ProtoActorCacheNode secondNode,
        CancellationToken cancellationToken)
    {
        var subscribers = (await subscribersStore.GetAsync(Topic, cancellationToken)).Subscribers_.ToArray();
        Assert.Equal(2, subscribers.Length);
        Assert.All(subscribers, subscriber => Assert.Equal(SubscriberIdentity.IdentityOneofCase.Pid, subscriber.IdentityCase));
        Assert.All(subscribers, subscriber => Assert.Equal(ActorName, subscriber.Pid.Id));
        Assert.Equal(
            new[] { firstNode.ActorSystem.Address, secondNode.ActorSystem.Address }.Order(),
            subscribers.Select(subscriber => subscriber.Pid.Address).Order());
        return subscribers;
    }

    private static Task<PublishResponse> PublishSignalAndFenceAsync(
        ProtoActorCacheNode node,
        string signalKey,
        string fenceKey,
        CancellationToken cancellationToken)
    {
        return node.Cluster.Publisher().PublishBatch(
            Topic,
            [
                new ProtoTriggerChangeTokenSignal { Key = signalKey },
                new ProtoTriggerChangeTokenSignal { Key = fenceKey }
            ],
            cancellationToken);
    }

    private static string NewClusterName() => $"cache-tests-{Guid.NewGuid():N}";
}

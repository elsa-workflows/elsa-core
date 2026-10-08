using Elsa.Actors.ProtoActor.Features;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;
using Proto.Utils;

namespace Elsa.Actors.ProtoActor.UnitTests.Features;

public class ProtoActorPubSubTests
{
    [Fact]
    public void Apply_CustomSubscribersStoreFactory_RegistersSingletonStore()
    {
        var store = new TestSubscribersStore();
        var invocationCount = 0;
        using var serviceProvider = CreateServiceProvider(feature => feature.CreatePubSubSubscribersStore = _ =>
        {
            invocationCount++;
            return store;
        });

        var first = serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>();
        var second = serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>();

        Assert.Same(store, first);
        Assert.Same(first, second);
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public void Apply_PreRegisteredSubscribersStore_PreservesRegistration()
    {
        var store = new TestSubscribersStore();
        using var serviceProvider = CreateServiceProvider(
            configureFeature: null,
            configureServices: services => services.AddSingleton<IKeyValueStore<Subscribers>>(store));

        Assert.Same(store, serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>());
    }

    [Fact]
    public async Task GetAsync_DefaultSubscribersStoreAfterSet_ReturnsStoredSubscribers()
    {
        using var serviceProvider = CreateServiceProvider();
        var store = serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>();
        var subscriber = new SubscriberIdentity { ClusterIdentity = ClusterIdentity.Create("subscriber", "kind") };
        var subscribers = new Subscribers { Subscribers_ = { subscriber } };

        await store.SetAsync("topic", subscribers, CancellationToken.None);
        var loaded = await store.GetAsync("topic", CancellationToken.None);

        Assert.Equal(subscriber, Assert.Single(loaded.Subscribers_));
        Assert.Same(store, serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>());
    }

    [Fact]
    public async Task DefaultSubscribersStore_CanceledOperations_DoNotReadOrMutateState()
    {
        using var serviceProvider = CreateServiceProvider();
        var store = serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>();
        var original = CreateSubscribers();
        var replacement = CreateSubscribers();
        replacement.Subscribers_[0].ClusterIdentity = ClusterIdentity.Create("replacement", "kind");
        await store.SetAsync("topic", original, CancellationToken.None);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledToken = cancellation.Token;

        await Assert.ThrowsAsync<OperationCanceledException>(() => store.GetAsync("topic", canceledToken));
        await Assert.ThrowsAsync<OperationCanceledException>(() => store.SetAsync("topic", replacement, canceledToken));
        await Assert.ThrowsAsync<OperationCanceledException>(() => store.ClearAsync("topic", canceledToken));

        var loaded = await store.GetAsync("topic", CancellationToken.None);
        Assert.Equal(original, loaded);
    }

    [Fact]
    public async Task Apply_NoCustomTopicKind_RegistersSingleTopicActorKind()
    {
        await using var serviceProvider = CreateServiceProvider();
        var actorSystem = serviceProvider.GetRequiredService<ActorSystem>();

        Assert.Single(actorSystem.Cluster().Config.ClusterKinds, x => x.Name == TopicActor.Kind);
    }

    [Fact]
    public async Task Apply_PreConfiguredTopicKind_PreservesRegistration()
    {
        var props = Props.FromFunc(_ => Task.CompletedTask);
        await using var serviceProvider = CreateServiceProvider(feature =>
            feature.ConfigureClusterConfig = (_, config) => config.WithClusterKind(TopicActor.Kind, props));
        var actorSystem = serviceProvider.GetRequiredService<ActorSystem>();

        var topicKind = Assert.Single(actorSystem.Cluster().Config.ClusterKinds, x => x.Name == TopicActor.Kind);
        Assert.Same(props, topicKind.Props);
    }

    [Fact]
    public async Task TopicActorProducer_StoreFactoryDependsOnCluster_ResolvesStoreLazily()
    {
        var store = new TestSubscribersStore();
        Cluster? resolvedCluster = null;
        await using var serviceProvider = CreateServiceProvider(feature => feature.CreatePubSubSubscribersStore = sp =>
        {
            resolvedCluster = sp.GetRequiredService<Cluster>();
            return store;
        });
        var actorSystem = serviceProvider.GetRequiredService<ActorSystem>();
        var cluster = actorSystem.Cluster();
        var topicKind = Assert.Single(cluster.Config.ClusterKinds, x => x.Name == TopicActor.Kind);

        Assert.Null(resolvedCluster);

        var context = Substitute.For<IContext>();
        context.Message.Returns(new SubscribeRequest
        {
            Subscriber = new SubscriberIdentity { ClusterIdentity = ClusterIdentity.Create("subscriber", "kind") }
        });
        var topicActor = topicKind.Props.Producer(actorSystem, context);
        await topicActor.ReceiveAsync(context);

        Assert.Same(cluster, resolvedCluster);
        Assert.Equal(1, store.SetInvocationCount);
    }

    private static ServiceProvider CreateServiceProvider(
        Action<ProtoActorFeature>? configureFeature = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        configureServices?.Invoke(services);

        var feature = new ProtoActorFeature(module);
        configureFeature?.Invoke(feature);
        feature.Apply();

        return services.BuildServiceProvider();
    }

    private static Subscribers CreateSubscribers() =>
        new()
        {
            Subscribers_ =
            {
                new SubscriberIdentity { ClusterIdentity = ClusterIdentity.Create("subscriber", "kind") }
            }
        };

    private sealed class TestSubscribersStore : IKeyValueStore<Subscribers>
    {
        public int SetInvocationCount { get; private set; }

        public Task<Subscribers> GetAsync(string id, CancellationToken ct) => Task.FromResult(new Subscribers());

        public Task SetAsync(string id, Subscribers state, CancellationToken ct)
        {
            SetInvocationCount++;
            return Task.CompletedTask;
        }

        public Task ClearAsync(string id, CancellationToken ct) => Task.CompletedTask;
    }
}

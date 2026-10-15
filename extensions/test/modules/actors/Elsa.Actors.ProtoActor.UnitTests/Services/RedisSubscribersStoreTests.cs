using Elsa.Actors.ProtoActor.Features;
using Elsa.Actors.ProtoActor.PubSub.Redis.Features;
using Elsa.Actors.ProtoActor.PubSub.Redis.Options;
using Elsa.Actors.ProtoActor.PubSub.Redis.Services;
using Elsa.Extensions;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Proto.Cluster;
using Proto.Cluster.PubSub;
using Proto.Utils;
using StackExchange.Redis;

namespace Elsa.Actors.ProtoActor.UnitTests.Services;

public class RedisSubscribersStoreTests
{
    [Fact]
    public async Task GetAsync_MissingKey_ReturnsEmptySubscribers()
    {
        var database = Substitute.For<IDatabase>();
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        var store = new RedisSubscribersStore("cluster", database);

        var subscribers = await store.GetAsync("topic", CancellationToken.None);

        Assert.Empty(subscribers.Subscribers_);
        await database.Received(1).StringGetAsync((RedisKey)"proto:cluster:pubsub:topic:topic", CommandFlags.None);
    }

    [Fact]
    public async Task GetAsync_StoredProtobuf_DeserializesSubscribers()
    {
        var database = Substitute.For<IDatabase>();
        var expected = CreateSubscribers();
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(expected.ToByteArray());
        var store = new RedisSubscribersStore("cluster", database);

        var actual = await store.GetAsync("topic", CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task SetAsync_SubscribersProvided_WritesProtobufToNamespacedKey()
    {
        var database = Substitute.For<IDatabase>();
        var expected = CreateSubscribers();
        var store = new RedisSubscribersStore("cluster", database);

        await store.SetAsync("topic", expected, CancellationToken.None);

        var call = Assert.Single(database.ReceivedCalls(), x => x.GetMethodInfo().Name == nameof(IDatabase.StringSetAsync));
        var arguments = call.GetArguments();
        Assert.Equal((RedisKey)"proto:cluster:pubsub:topic:topic", (RedisKey)arguments[0]!);
        Assert.Equal(expected, Subscribers.Parser.ParseFrom((RedisValue)arguments[1]!));
    }

    [Fact]
    public async Task ClearAsync_TopicProvided_DeletesNamespacedKey()
    {
        var database = Substitute.For<IDatabase>();
        var store = new RedisSubscribersStore("cluster", database);

        await store.ClearAsync("topic", CancellationToken.None);

        await database.Received(1).KeyDeleteAsync((RedisKey)"proto:cluster:pubsub:topic:topic", CommandFlags.None);
    }

    [Fact]
    public async Task CanceledOperations_DoNotCallRedis()
    {
        var database = Substitute.For<IDatabase>();
        var store = new RedisSubscribersStore("cluster", database);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => store.GetAsync("topic", cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => store.SetAsync("topic", CreateSubscribers(), cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => store.ClearAsync("topic", cancellation.Token));

        Assert.Empty(database.ReceivedCalls());
    }

    [Fact]
    public async Task ClearAsync_CustomKeyPrefix_UsesCustomNamespace()
    {
        var database = Substitute.For<IDatabase>();
        var options = new RedisSubscribersStoreOptions { KeyPrefix = "elsa" };
        var store = new RedisSubscribersStore("cluster", database, options);

        await store.ClearAsync("topic", CancellationToken.None);

        await database.Received(1).KeyDeleteAsync((RedisKey)"elsa:cluster:pubsub:topic:topic", CommandFlags.None);
    }

    [Fact]
    public async Task ClearAsync_CustomKeyFormatter_UsesFormatterAndIgnoresPrefix()
    {
        var database = Substitute.For<IDatabase>();
        var options = new RedisSubscribersStoreOptions
        {
            KeyPrefix = "ignored",
            KeyFormatter = (clusterName, topic) => $"custom:{topic}:{clusterName}"
        };
        var store = new RedisSubscribersStore("cluster", database, options);

        await store.ClearAsync("topic", CancellationToken.None);

        await database.Received(1).KeyDeleteAsync((RedisKey)"custom:topic:cluster", CommandFlags.None);
    }

    [Fact]
    public async Task ClearAsync_OptionsMutatedAfterConstruction_UsesInitialOptionsSnapshot()
    {
        var database = Substitute.For<IDatabase>();
        var options = new RedisSubscribersStoreOptions { KeyPrefix = "initial" };
        var store = new RedisSubscribersStore("cluster", database, options);

        options.KeyPrefix = "changed";
        await store.ClearAsync("topic", CancellationToken.None);

        await database.Received(1).KeyDeleteAsync((RedisKey)"initial:cluster:pubsub:topic:topic", CommandFlags.None);
    }

    [Fact]
    public async Task UseRedisPubSubSubscribersStore_CustomConfiguration_ConfiguresCoreProtoActorFeature()
    {
        var database = Substitute.For<IDatabase>();
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var feature = module.Configure<ProtoActorFeature>(x => x.ClusterName = "cluster");

        var result = feature.UseRedisPubSubSubscribersStore(redis =>
        {
            redis.CreateDatabase = _ => database;
            redis.ConfigureOptions = options => options.KeyPrefix = "elsa";
        });
        module.Apply();
        using var serviceProvider = services.BuildServiceProvider();
        var store = serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>();
        await store.ClearAsync("topic", CancellationToken.None);

        Assert.Same(feature, result);
        Assert.True(module.HasFeature<RedisPubSubSubscribersStoreFeature>());
        Assert.IsType<RedisSubscribersStore>(store);
        await database.Received(1).KeyDeleteAsync((RedisKey)"elsa:cluster:pubsub:topic:topic", CommandFlags.None);
    }

    [Fact]
    public void Configure_RedisFeatureConfiguredDirectly_InstallsProtoActorDependency()
    {
        var database = Substitute.For<IDatabase>();
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.Configure<RedisPubSubSubscribersStoreFeature>(feature => feature.CreateDatabase = _ => database);

        module.Apply();
        using var serviceProvider = services.BuildServiceProvider();

        Assert.True(module.HasFeature<ProtoActorFeature>());
        Assert.IsType<RedisSubscribersStore>(serviceProvider.GetRequiredService<IKeyValueStore<Subscribers>>());
    }

    private static Subscribers CreateSubscribers() =>
        new()
        {
            Subscribers_ =
            {
                new SubscriberIdentity { ClusterIdentity = ClusterIdentity.Create("subscriber", "kind") }
            }
        };
}

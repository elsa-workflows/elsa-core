using Elsa.Caching.Distributed.Features;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;
using Proto.Cluster.Testing;

namespace Elsa.Caching.Distributed.ProtoActor.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts one Elsa distributed-cache member backed by a real Proto.Actor <see cref="ActorSystem"/>.
/// </summary>
/// <remarks>
/// Multiple nodes form an in-process cluster when they share the same cluster name, <see cref="InMemAgent"/>,
/// and <see cref="TestSubscribersStore"/>.
/// </remarks>
internal sealed class ProtoActorCacheNode : IAsyncDisposable
{
    private readonly IHost _host;
    private bool _started;

    private ProtoActorCacheNode(IHost host, RecordingChangeTokenSignalInvoker signalInvoker)
    {
        _host = host;
        SignalInvoker = signalInvoker;
    }

    /// <summary>
    /// Gets the actor system owned by this node.
    /// </summary>
    public ActorSystem ActorSystem => _host.Services.GetRequiredService<ActorSystem>();

    /// <summary>
    /// Gets the cluster extension attached to this node's actor system.
    /// </summary>
    public Cluster Cluster => _host.Services.GetRequiredService<Cluster>();

    /// <summary>
    /// Gets the recorder used to observe cache invalidations delivered to this node.
    /// </summary>
    public RecordingChangeTokenSignalInvoker SignalInvoker { get; }

    /// <summary>
    /// Creates a configured, but not yet started, cluster member.
    /// </summary>
    /// <param name="clusterName">The cluster name shared by participating nodes.</param>
    /// <param name="agent">The in-memory discovery agent shared by participating nodes.</param>
    /// <param name="subscribersStore">The Pub/Sub subscriber store shared by participating nodes.</param>
    /// <returns>A node whose host lifecycle controls its cluster member and local cache invalidator.</returns>
    public static ProtoActorCacheNode Create(string clusterName, InMemAgent agent, TestSubscribersStore subscribersStore)
    {
        var signalInvoker = new RecordingChangeTokenSignalInvoker();
        var builder = Host.CreateApplicationBuilder();
        var module = builder.Services.CreateModule();

        module.UseProtoActor(feature =>
        {
            feature.ClusterName = clusterName;
            feature.CreateClusterProvider = _ => new TestProvider(new TestProviderOptions(), agent);
            feature.ConfigureClusterConfig = (_, config) => config
                .WithActorRequestTimeout(TimeSpan.FromSeconds(5))
                .WithActorSpawnVerificationTimeout(TimeSpan.FromSeconds(5))
                .WithActorActivationTimeout(TimeSpan.FromSeconds(5))
                .WithGossipRequestTimeout(TimeSpan.FromSeconds(5))
                .WithPubSubConfig(PubSubConfig.Setup().WithSubscriberTimeout(TimeSpan.FromSeconds(5)))
                .WithClusterKind(new ClusterKind(TopicActor.Kind, Props.FromProducer(() => new TopicActor(subscribersStore))));
        });
        module.Use<DistributedCacheFeature>(feature => feature.UseProtoActor());
        module.Apply();

        builder.Services.RemoveAll<IChangeTokenSignalInvoker>();
        builder.Services.AddSingleton<IChangeTokenSignalInvoker>(signalInvoker);

        return new ProtoActorCacheNode(builder.Build(), signalInvoker);
    }

    /// <summary>
    /// Starts the host and waits until all hosted services, including the cache invalidator subscription, have started.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _host.StartAsync(cancellationToken);
        _started = true;
    }

    /// <summary>
    /// Stops the host, allowing the cache invalidator to unsubscribe and terminate before cluster shutdown.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_started)
            return;

        try
        {
            await _host.StopAsync(cancellationToken);
        }
        finally
        {
            _started = false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_started)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await StopAsync(cancellationTokenSource.Token);
        }

        await ((IAsyncDisposable)_host).DisposeAsync();
    }
}

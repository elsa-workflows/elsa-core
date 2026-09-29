using System.Collections.Concurrent;
using Elsa.Actors.ProtoActor.Features;
using Elsa.Actors.ProtoActor.HostedServices;
using Elsa.Caching.Distributed.ProtoActor.Features;
using Elsa.Caching.Distributed.ProtoActor.HostedServices;
using Elsa.Common.Multitenancy.HostedServices;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace Elsa.Actors.ProtoActor.UnitTests.Features;

public class ProtoActorFeatureTests
{
    [Fact]
    public void StartClusterMember_IsRegisteredBeforeDependentHostedServices()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.ConfigureHostedService<DependentHostedService>(-1);
        new ProtoActorFeature(module).ConfigureHostedServices();

        module.Apply();

        var hostedServices = services.Where(x => x.ServiceType == typeof(IHostedService)).ToList();
        Assert.Collection(
            hostedServices,
            descriptor => Assert.Equal(typeof(StartClusterMember), descriptor.ImplementationType),
            descriptor => Assert.Equal(typeof(DependentHostedService), descriptor.ImplementationType));
    }

    [Fact]
    public void StartClusterMember_IsRegisteredAfterMongoSerializerInitialization()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.ConfigureHostedService<MongoSerializerInitializationHostedService>(-10);
        module.ConfigureHostedService<DependentHostedService>(-1);
        new ProtoActorFeature(module).ConfigureHostedServices();

        module.Apply();

        var hostedServices = services
            .Where(x => x.ServiceType == typeof(IHostedService))
            .Select(x => x.ImplementationType)
            .ToList();

        Assert.Equal(
            [
                typeof(MongoSerializerInitializationHostedService),
                typeof(StartClusterMember),
                typeof(DependentHostedService)
            ],
            hostedServices);
    }

    [Fact]
    public void StartClusterMember_IsRegisteredBeforeTenantActivationAndLocalCacheActor()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.ConfigureHostedService<ActivateTenants>(-1);
        module.Configure<ProtoActorFeature>();
        module.Configure<ProtoActorDistributedCacheFeature>();

        module.Apply();

        var hostedServices = services
            .Where(x => x.ServiceType == typeof(IHostedService))
            .Select(x => x.ImplementationType)
            .ToList();
        var clusterMemberIndex = hostedServices.IndexOf(typeof(StartClusterMember));
        var tenantActivationIndex = hostedServices.IndexOf(typeof(ActivateTenants));
        var localCacheIndex = hostedServices.IndexOf(typeof(StartLocalCacheActor));

        Assert.True(clusterMemberIndex >= 0);
        Assert.True(tenantActivationIndex >= 0);
        Assert.True(localCacheIndex >= 0);
        Assert.True(clusterMemberIndex < tenantActivationIndex);
        Assert.True(clusterMemberIndex < localCacheIndex);
    }

    [Fact]
    public async Task ClusterDependentHostServiceCanSubscribeAndStopsBeforeClusterShutdown()
    {
        var lifecycle = new LifecycleEvents();
        using var host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddLogging();
                services.AddSingleton(lifecycle);

                var module = services.CreateModule();
                module.Configure<ProtoActorFeature>();
                module.Configure<ProtoActorDistributedCacheFeature>();
                module.ConfigureHostedService<ClusterSubscriptionHostedService>(-1);
                module.Apply();
            })
            .Build();

        var cleanupNeeded = true;

        try
        {
            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await host.StartAsync(startTimeout.Token);

            var probe = host.Services.GetServices<IHostedService>().OfType<ClusterSubscriptionHostedService>().Single();
            Assert.True(probe.SubscriptionSucceeded);

            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await host.StopAsync(stopTimeout.Token);
            cleanupNeeded = false;

            Assert.True(probe.ClusterWasRunningWhenStopped);
            Assert.Equal(["start", "subscribed", "stop"], lifecycle.Events);
            Assert.True(host.Services.GetRequiredService<Cluster>().ShutdownCompleted.IsCompletedSuccessfully);
        }
        finally
        {
            if (cleanupNeeded)
            {
                using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await host.StopAsync(cleanupTimeout.Token);
            }
        }
    }

    private sealed class DependentHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class MongoSerializerInitializationHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ClusterSubscriptionHostedService(ActorSystem actorSystem, Cluster cluster, LifecycleEvents lifecycle) : IHostedService
    {
        private const string Topic = "proto-actor-startup-order-tests";
        private PID? _subscriber;

        public bool SubscriptionSucceeded { get; private set; }
        public bool ClusterWasRunningWhenStopped { get; private set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            lifecycle.Events.Enqueue("start");
            _subscriber = actorSystem.Root.Spawn(Props.FromFunc(_ => Task.CompletedTask));
            await cluster.Subscribe(Topic, _subscriber, cancellationToken);
            SubscriptionSucceeded = true;
            lifecycle.Events.Enqueue("subscribed");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ClusterWasRunningWhenStopped = !cluster.ShutdownCompleted.IsCompleted;
            lifecycle.Events.Enqueue("stop");
            if (SubscriptionSucceeded && _subscriber != null)
            {
                try
                {
                    await cluster.Unsubscribe(Topic, _subscriber, cancellationToken);
                }
                finally
                {
                    _subscriber.Stop(actorSystem);
                }
            }
            else
                _subscriber?.Stop(actorSystem);
        }
    }

    private sealed class LifecycleEvents
    {
        public ConcurrentQueue<string> Events { get; } = new();
    }
}

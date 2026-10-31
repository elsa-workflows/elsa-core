using Elsa.Actors.ProtoActor.Features;
using Elsa.Extensions;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using Proto.Remote;

namespace Elsa.Actors.ProtoActor.UnitTests.Features;

public class ProtoActorRemoteConfigTests
{
    [Fact]
    public async Task ConfigureRemoteConfig_IsInvokedOnce_WhenResolvingActorSystem()
    {
        var invocationCount = 0;
        await using var serviceProvider = CreateServiceProvider(_ =>
        {
            invocationCount++;
            return RemoteConfig.BindToLocalhost();
        });

        _ = serviceProvider.GetRequiredService<ActorSystem>();

        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task VirtualActorDescriptors_AreRegisteredOnAttachedRemoteConfig_WhenResolvingActorSystem()
    {
        RemoteConfig? configuredRemoteConfig = null;
        await using var serviceProvider = CreateServiceProvider(_ =>
        {
            configuredRemoteConfig = RemoteConfig.BindToLocalhost();
            return configuredRemoteConfig;
        });
        var actorSystem = serviceProvider.GetRequiredService<ActorSystem>();
        var attachedSerialization = actorSystem.Remote().Config.Serialization;

        Assert.Same(configuredRemoteConfig, actorSystem.Remote().Config);
        Assert.Same(attachedSerialization, actorSystem.Serialization());

        var (_, typeName, serializerId) = attachedSerialization.Serialize(new StringValue { Value = "repro" });

        Assert.Equal(StringValue.Descriptor.FullName, typeName);
        Assert.Equal(Serialization.SERIALIZER_ID_PROTOBUF, serializerId);
    }

    private static ServiceProvider CreateServiceProvider(Func<IServiceProvider, RemoteConfig> configureRemoteConfig)
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        services.AddSingleton<IVirtualActorsProvider>(new DescriptorVirtualActorsProvider());

        var feature = new ProtoActorFeature(module)
        {
            ConfigureRemoteConfig = configureRemoteConfig
        };

        feature.Apply();

        return services.BuildServiceProvider();
    }

    private sealed class DescriptorVirtualActorsProvider : VirtualActorsProviderBase
    {
        public override IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system) => [];

        public override IEnumerable<FileDescriptor> GetFileDescriptors() => [StringValue.Descriptor.File];
    }
}

using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Elsa.Caching.Distributed.ProtoActor.Providers;
using Proto;

namespace Elsa.Caching.Distributed.ProtoActor.IntegrationTests.Scenarios;

public class LocalCacheVirtualActorProviderTests
{
    [Fact]
    public async Task GetClusterKinds_WhenLegacyActorIsRemoved_ReturnsEmpty()
    {
        await using var actorSystem = new ActorSystem();
        var provider = new LocalCacheVirtualActorProvider();

        Assert.Empty(provider.GetClusterKinds(actorSystem));
    }

    [Fact]
    public void GetFileDescriptors_WhenLegacyActorIsRemoved_ReturnsCacheMessageDescriptor()
    {
        var provider = new LocalCacheVirtualActorProvider();

        Assert.Same(LocalCacheMessagesReflection.Descriptor, Assert.Single(provider.GetFileDescriptors()));
    }
}

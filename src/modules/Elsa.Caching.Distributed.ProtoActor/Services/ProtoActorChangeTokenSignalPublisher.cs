using Elsa.Caching.Distributed.Contracts;

namespace Elsa.Caching.Distributed.ProtoActor.Services;

public class ProtoActorChangeTokenSignalPublisher : IChangeTokenSignalPublisher
{
    public ValueTask PublishAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
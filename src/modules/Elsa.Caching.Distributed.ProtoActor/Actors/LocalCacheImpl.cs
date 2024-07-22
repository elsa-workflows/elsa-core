using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Microsoft.Extensions.DependencyInjection;
using Proto;

namespace Elsa.Caching.Distributed.ProtoActor.Actors;

internal class LocalCacheImpl(IContext context, IServiceScopeFactory scopeFactory) : LocalCacheBase(context)
{
}
using Elsa.Features.Services;
using Elsa.Actors.ProtoActor.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

[PublicAPI]
public static class ProtoActorModuleExtensions
{
    /// <summary>
    /// Creates and configures a new ActorSystem.
    /// </summary>
    public static IModule UseProtoActor(this IModule module, Action<ProtoActorFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
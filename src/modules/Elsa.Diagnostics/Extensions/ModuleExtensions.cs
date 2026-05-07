using Elsa.Diagnostics.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServerLogStreamingModuleExtensions
{
    public static IModule UseServerLogStreaming(this IModule module, Action<ServerLogStreamingFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}

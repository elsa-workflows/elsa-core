using Elsa.Features.Services;

namespace Elsa.Agents;

public static class ModuleExtensions
{
    public static IModule UseAgents(this IModule module, Action<AgentsFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
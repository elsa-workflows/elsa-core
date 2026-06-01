using Elsa.Features.Services;
using Elsa.ModularPersistence.Features;

namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseModularPersistence(this IModule module, Action<ModularPersistenceFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}

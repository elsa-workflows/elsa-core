using Elsa.Features.Services;
using Elsa.Persistence.VNext.Extensions.Features;

namespace Elsa.Persistence.VNext.Extensions;

public static class ModuleExtensions
{
    public static IModule UsePersistenceVNext(this IModule module, Action<PersistenceVNextFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}

using Elsa.Features.Services;
using Elsa.Telnyx.Features;

namespace Elsa.Telnyx.Extensions;

public static class ModuleExtensions
{
    public static IModule UseTelnyx(this IModule module, Action<TelnyxFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.IO.Features;

namespace Elsa.IO.Extensions;

/// <summary>
/// Provides extension methods for configuring IO services.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the IO module.
    /// </summary>
    public static IModule UseIO(this IModule module, Action<IOFeature>? configure = default)
    {
        return module.Use(configure);
    }
}
using Elsa.Features.Services;
using Elsa.Identity.Features;

namespace Elsa.Identity.Extensions;

/// <summary>
/// Extensions for <see cref="IModule"/> that installs the <see cref="IdentityFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs & configures the <see cref="IdentityFeature"/> feature.
    /// </summary>
    public static IModule UseIdentity(this IModule module, Action<IdentityFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Ldap.Features;

namespace Elsa.Ldap.Extensions;

/// <summary>
/// Provides extension methods for configuring LDAP services.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the LDAP module.
    /// </summary>
    public static IModule UseLdap(this IModule module, Action<LdapFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
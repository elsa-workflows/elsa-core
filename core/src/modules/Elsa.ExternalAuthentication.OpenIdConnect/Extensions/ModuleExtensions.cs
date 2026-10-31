using Elsa.ExternalAuthentication.OpenIdConnect.Features;
using Elsa.Features.Services;

namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseOpenIdConnectExternalAuthentication(
        this IModule module,
        Action<OpenIdConnectExternalAuthenticationFeature>? configure = null)
    {
        return module.Use(configure);
    }
}

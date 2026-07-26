using Elsa.ExternalAuthentication.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.OpenIdConnect.Features;

[DependsOn(typeof(ExternalAuthenticationFeature))]
public sealed class OpenIdConnectExternalAuthenticationFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddOpenIdConnectExternalAuthentication();
    }
}

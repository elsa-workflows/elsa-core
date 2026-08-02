using Elsa.ExternalAuthentication.Options;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Features;

[DependsOn(typeof(SystemClockFeature))]
public sealed class ExternalAuthenticationFeature(IModule module) : FeatureBase(module)
{
    /// <summary>Configures deployment-owned broker clients, connections, security boundaries, and operations.</summary>
    public Action<ExternalAuthenticationOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ExternalAuthenticationFeature>();
    }

    public override void Apply()
    {
        Services.AddExternalAuthenticationServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}

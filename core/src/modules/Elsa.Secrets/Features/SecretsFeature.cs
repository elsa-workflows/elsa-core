using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Secrets.Extensions;

namespace Elsa.Secrets.Features;

public class SecretsFeature(IModule module) : FeatureBase(module)
{
    public Action<SecretsOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<SecretsFeature>();
    }

    public override void Apply()
    {
        Services.AddSecretsServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}

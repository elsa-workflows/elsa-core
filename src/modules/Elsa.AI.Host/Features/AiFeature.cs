using Elsa.AI.Host.Extensions;
using Elsa.AI.Host.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.AI.Host.Features;

public class AiFeature(IModule module) : FeatureBase(module)
{
    public Action<AiHostOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<AiFeature>();
    }

    public override void Apply()
    {
        Services.AddAiHostServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}

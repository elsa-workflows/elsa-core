using Elsa.AI.Host.Extensions;
using Elsa.AI.Host.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.AI.Host.Features;

public class AIFeature(IModule module) : FeatureBase(module)
{
    public Action<AIHostOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<AIFeature>();
    }

    public override void Apply()
    {
        Services.AddAIHostServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}

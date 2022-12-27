using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.MassTransit.Features;

public class MassTransitFeature : FeatureBase
{
    public MassTransitFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        Module.AddMassTransitFromModule();
    }
}
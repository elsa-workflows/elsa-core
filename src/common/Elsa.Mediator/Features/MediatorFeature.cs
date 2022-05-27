using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;

namespace Elsa.Mediator.Features;

public class MediatorFeature : FeatureBase
{
    public MediatorFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        Services.AddMediator();
    }
}
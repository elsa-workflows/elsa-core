using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

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
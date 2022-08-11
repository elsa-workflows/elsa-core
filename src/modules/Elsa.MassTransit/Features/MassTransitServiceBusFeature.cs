using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MassTransit.Implementations;
using Elsa.ServiceBus.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Features;

public class MassTransitServiceBusFeature : FeatureBase
{
    public MassTransitServiceBusFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        Services.AddSingleton<IServiceBus, MassTransitServiceBus>();
    }
}
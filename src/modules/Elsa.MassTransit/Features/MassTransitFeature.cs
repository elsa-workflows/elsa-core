using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using MassTransit;

namespace Elsa.MassTransit.Features;

public class MassTransitFeature : FeatureBase
{
    public MassTransitFeature(IModule module) : base(module)
    {
    }

    public Action<IBusRegistrationConfigurator> BusConfigurator { get; set; } = default!;

    public override void Configure()
    {
        BusConfigurator = configure =>
        {
            configure.UsingInMemory((context, configurator) =>
            {
                configurator.ConfigureEndpoints(context);
            });
        };
    }

    public override void Apply()
    {
        Module.AddMassTransitFromModule(BusConfigurator);
    }
}
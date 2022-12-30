using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MassTransit.Extensions;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace Elsa.MassTransit.Features;

public class MassTransitFeature : FeatureBase
{
    public MassTransitFeature(IModule module) : base(module)
    {
    }

    public Action<IBusRegistrationConfigurator>? BusConfigurator { get; set; }

    public IConfiguration Configuration { get; set; }

    public override void Configure()
    {
        BusConfigurator ??= configure =>
        {
            configure.UsingInMemory((context, configurator) => { configurator.ConfigureEndpoints(context); });
        };
    }

    public override void Apply()
    {
        Module.AddMassTransitFromModule(BusConfigurator);
    }
}
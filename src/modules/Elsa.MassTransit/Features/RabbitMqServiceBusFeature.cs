using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Options;
using MassTransit;

namespace Elsa.MassTransit.Features;

[DependsOn(typeof(MassTransitFeature))]
public class RabbitMqServiceBusFeature : FeatureBase
{
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }

    public string ConnectionString { get; set; }
    
    public RabbitMqOptions Options { get; set; }

    public override void Configure()
    {
        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                var host = new Uri("rabbitmq://" + ConnectionString);

                configurator.Host(host, h =>
                {
                    h.Username(Options.Username);
                    h.Password(Options.Password);
                });

                configurator.ConfigureEndpoints(context);
            });
        };
    }
}
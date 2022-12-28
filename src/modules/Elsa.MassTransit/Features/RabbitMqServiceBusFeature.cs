using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.Features;

[DependsOn(typeof(MassTransitFeature))]
public class RabbitMqServiceBusFeature : FeatureBase
{
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        var serviceProvider = Services.BuildServiceProvider();
        
        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                var rabbitMqSettings = serviceProvider.GetService<IOptions<RabbitMqOptions>>()!.Value;
                var host = new Uri("rabbitmq://" + rabbitMqSettings.ConnectionString);

                configurator.Host(host, h =>
                {
                    h.Username(rabbitMqSettings.Username);
                    h.Password(rabbitMqSettings.Password);
                });

                configurator.ConfigureEndpoints(context);
            });
        };
    }
}
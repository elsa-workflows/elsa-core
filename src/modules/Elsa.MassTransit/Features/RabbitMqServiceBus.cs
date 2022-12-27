using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.Features;

public class RabbitMqServiceBusFeature : MassTransitFeature
{
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        var serviceProvider = Services.BuildServiceProvider();
        
        Module.AddMassTransitFromModule(configure =>
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                var rabbitMqSettings = serviceProvider.GetService<IOptions<RabbitMqOptions>>()!.Value;
                var host = new Uri("rabbitmq://" + rabbitMqSettings.ConnectionString);

                configurator.Host(host, h =>
                {
                    h.Username(rabbitMqSettings.UserName);
                    h.Password(rabbitMqSettings.Password);
                });

                configurator.ConfigureEndpoints(context);
            });
        });
    }
}
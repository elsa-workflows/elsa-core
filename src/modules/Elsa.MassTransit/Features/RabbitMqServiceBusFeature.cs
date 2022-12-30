using System.Diagnostics.CodeAnalysis;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace Elsa.MassTransit.Features;

[DependsOn(typeof(MassTransitFeature))]
public class RabbitMqServiceBusFeature : FeatureBase
{
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }
    
    public override void Configure()
    {
        var configuration = Module.Configure<MassTransitFeature>().Configuration;

        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            var rabbitMqOptions = new RabbitMqOptions();
            configuration.GetSection(RabbitMqOptions.RabbitMq).Bind(rabbitMqOptions);
            
            configure.UsingRabbitMq((context, configurator) =>
            {
                var connectionString = configuration.GetConnectionString(RabbitMqOptions.RabbitMq);
                var host = new Uri("rabbitmq://" + connectionString);

                configurator.Host(host, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                configurator.ConfigureEndpoints(context);
            });
        };
    }
}
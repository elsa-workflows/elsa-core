using System.Diagnostics.CodeAnalysis;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Configuration;
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
        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            var rabbitMqSettings = new RabbitMqOptions();
            var configuration = configure.BuildServiceProvider().GetService<IConfiguration>();
            configuration.GetSection(RabbitMqOptions.RabbitMq).Bind(rabbitMqSettings);

            configure.UsingRabbitMq((context, configurator) =>
            {
                var connectionString = configuration.GetConnectionString(RabbitMqOptions.RabbitMq);
                var host = new Uri("rabbitmq://" + connectionString);

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
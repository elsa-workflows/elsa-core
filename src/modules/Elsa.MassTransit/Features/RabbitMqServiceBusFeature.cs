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
    private readonly IConfiguration _configuration;
    
    public RabbitMqServiceBusFeature(IModule module, IConfiguration configuration) : base(module)
    {
        _configuration = configuration;
    }
    
    public override void Configure()
    {
        var rabbitMqSettings = new RabbitMqOptions();
        _configuration.GetSection(RabbitMqOptions.RabbitMq).Bind(rabbitMqSettings);

        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                var connectionString = _configuration.GetConnectionString(RabbitMqOptions.RabbitMq);
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
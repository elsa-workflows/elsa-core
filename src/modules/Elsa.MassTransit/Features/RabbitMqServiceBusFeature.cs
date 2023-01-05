using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Options;
using MassTransit;

namespace Elsa.MassTransit.Features;

/// <summary>
/// Configures MassTransit to use the RabbitMQ broker.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
public class RabbitMqServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A RabbitMQ connection string.
    /// </summary>
    public Uri? ConnectionString { get; set; }

    /// <summary>
    /// RabbitMQ options.
    /// </summary>
    public RabbitMqOptions? Options { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                if (ConnectionString != null)
                {
                    configurator.Host(ConnectionString);
                }
                else if (Options != null)
                {
                    configurator.Host(Options.Host, h =>
                    {
                        h.Username(Options.Username);
                        h.Password(Options.Password);
                    });
                }

                configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
            });
        };
    }
}
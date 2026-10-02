using CShells.Configuration;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.RabbitMq.Configurators;
using Elsa.ServiceBus.MassTransit.RabbitMq.Options;
using Elsa.ServiceBus.MassTransit.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.RabbitMq.ShellFeatures;

/// <summary>
/// Configures MassTransit to use RabbitMQ as the transport.
/// </summary>
/// <remarks>
/// Registers a <see cref="RabbitMqTransportConfigurator"/> as the
/// <see cref="IBusTransportConfigurator"/> singleton, replacing the default in-memory
/// configurator registered by <see cref="MassTransitFeature"/>.
/// <see cref="MassTransitFeature.PostConfigureServices"/> then calls
/// <c>AddMassTransit</c> exactly once, picking up this configurator.
/// </remarks>
[ShellFeature(
    DisplayName = "MassTransit RabbitMQ Transport",
    Description = "Configures MassTransit to use RabbitMQ as the message transport",
    DependsOn = [typeof(MassTransitFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("rabbitmq-broker", "message-broker", Reason = "Configures MassTransit to use RabbitMQ as its transport.", Providers = new[] { "RabbitMQ" }, ConfigurationKeys = new[] { "MassTransitRabbitMq" })]
public class MassTransitRabbitMqFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<RabbitMqOptions>()
            .Configure<ShellConfiguration>((options, config) =>
                config.GetSection("MassTransitRabbitMq").Bind(options));

        // Replace the default InMemoryTransportConfigurator.
        // MassTransitFeature.PostConfigureServices will pick this up via IServiceCollection
        // after all features have run, then call AddMassTransit exactly once.
        services.AddSingleton<IBusTransportConfigurator, RabbitMqTransportConfigurator>();
    }
}

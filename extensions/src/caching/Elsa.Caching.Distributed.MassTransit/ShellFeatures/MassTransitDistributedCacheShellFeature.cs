using CShells.Features;
using Elsa.Caching.Distributed.MassTransit.Consumers;
using Elsa.Caching.Distributed.MassTransit.Services;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Caching.Distributed.MassTransit.ShellFeatures;

/// <summary>
/// Configures distributed cache management with MassTransit.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Distributed Cache",
    Description = "Configures MassTransit-based distributed cache change-token signalling",
    DependsOn = [typeof(MassTransitFeature)])]
[UsedImplicitly]
public class MassTransitDistributedCacheShellFeature(ShellFeatureContext context) : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        context.AddMassTransitConsumer<TriggerChangeTokenSignalConsumer>(
            endpointName: "elsa-trigger-change-token-signal",
            isTemporary: true,
            ignoreConsumersDisabled: true);

        services.AddSingleton<MassTransitChangeTokenSignalPublisher>();
    }
}


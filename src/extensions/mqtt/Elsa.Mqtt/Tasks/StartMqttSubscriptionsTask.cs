using Elsa.Common;
using Elsa.Mqtt.Services;
using JetBrains.Annotations;

namespace Elsa.Mqtt.Tasks;

/// <summary>
/// Startup task that initialises MQTT topic subscriptions for all registered
/// workflow triggers and bookmarks at application start.
/// </summary>
[UsedImplicitly]
internal class StartMqttSubscriptionsTask(IMqttSubscriberManager subscriberManager) : BackgroundTask
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return subscriberManager.UpdateSubscriptionsAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        subscriberManager.StopAll();

        return Task.CompletedTask;
    }
}

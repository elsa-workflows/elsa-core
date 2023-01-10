using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.Options;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Rebus.AzureServiceBus;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Rebus.AzureServiceBus.StartupTasks
{
    /// <summary>
    /// This startup Task remove Azure Service Bus Subscription 
    /// that are not linked to any Azure Service Bus Queue (which are deleted after 5 min of inactivity)
    /// </summary>
    public class PurgeSubscriptions : IStartupTask
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ServiceBusAdministrationClient _managementClient;

        public int Order => 1000;

        public PurgeSubscriptions(
            IServiceBusFactory serviceBusFactory,
            ElsaOptions elsaOptions,
            ILogger logger,
            IDistributedLockProvider distributedLockProvider,
            string connectionString)
        {
            _serviceBusFactory = serviceBusFactory;
            _elsaOptions = elsaOptions;
            _logger = logger;
            _distributedLockProvider = distributedLockProvider;
            _managementClient = new ServiceBusAdministrationClient(connectionString);
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(nameof(PurgeSubscriptions), _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                throw new Exception("Could not acquire a lock within the maximum amount of time configured");

            var messageTypes = _elsaOptions.PubSubMessageTypes;

            var convention = new DefaultAzureServiceBusTopicNameConvention();

            var topics = messageTypes.Select(x => convention.GetTopic(x.MessageType)).Distinct();
            var client = _managementClient;
            foreach (var topic in topics)
            {
                var subscriptions = client.GetSubscriptionsAsync(topic);
                await foreach (var subscription in subscriptions)
                {
                    try
                    {
                        await client.GetQueueAsync(subscription.SubscriptionName);
                        _logger.LogInformation("Subscription {Name} still active", subscription.SubscriptionName);
                    }
                    catch (ServiceBusException e) when (e.Reason ==
                                                        ServiceBusFailureReason.MessagingEntityNotFound)
                    {
                        _logger.LogInformation("Found stale subscription {Name}, deleting...", subscription.SubscriptionName);
                        try
                        {
                            await client.DeleteSubscriptionAsync(topic, subscription.SubscriptionName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete subscription, ignoring...");
                        }
                    }
                }
            }
        }
    }
}

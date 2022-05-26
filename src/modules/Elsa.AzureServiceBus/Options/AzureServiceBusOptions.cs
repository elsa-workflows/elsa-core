using Elsa.AzureServiceBus.Models;

namespace Elsa.AzureServiceBus.Options;

public class AzureServiceBusOptions
{
    public string ConnectionStringOrName { get; set; } = default!;
    public ICollection<QueueDefinition> Queues { get; set; } = new List<QueueDefinition>();
    public ICollection<TopicDefinition> Topics { get; set; } = new List<TopicDefinition>();
    public ICollection<SubscriptionDefinition> Subscriptions { get; set; } = new List<SubscriptionDefinition>();
}
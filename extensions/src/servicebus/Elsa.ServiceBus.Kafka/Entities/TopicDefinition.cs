using Elsa.Common.Entities;

namespace Elsa.ServiceBus.Kafka;

public class TopicDefinition : Entity
{
    /// <summary>
    /// Gets or sets the name of the topic.
    /// </summary>
    public string Name { get; set; } = null!;
}
using Elsa.Common.Entities;

namespace Elsa.Kafka;

public class TopicDefinition : Entity
{
    /// <summary>
    /// Gets or sets the name of the topic.
    /// </summary>
    public string Name { get; set; } = default!;
}
namespace Elsa.ServiceBus.Kafka;

public class KafkaOptions
{
    public ICollection<TopicDefinition> Topics { get; set; } = [];
    public ICollection<SchemaRegistryDefinition> SchemaRegistries { get; set; } = [];
    public ICollection<ConsumerDefinition> Consumers { get; set; } = [];
    public ICollection<ProducerDefinition> Producers { get; set; } = [];
    public string WorkflowInstanceIdHeaderKey { get; set; } = "x-workflow-instance-id";
    public string CorrelationHeaderKey { get; set; } = "x-correlation-id";
    public string TenantHeaderKey { get; set; } = "Tenant";

    /// <summary>
    /// Optional prefix filter for the Schema Full Name dropdown. When set, only schemas whose full name
    /// starts with this value are shown. Leave null or empty to show all schemas.
    /// Example: <c>Event_</c>
    /// </summary>
    public string? SchemaFullNamePrefix { get; set; }
}

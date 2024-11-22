namespace Elsa.Kafka;

public class KafkaOptions
{
    public ICollection<TopicDefinition> Topics { get; set; } = [];
    public ICollection<ConsumerDefinition> Consumers { get; set; } = [];
    public ICollection<ProducerDefinition> Producers { get; set; } = [];
    public string WorkflowInstanceIdHeaderKey { get; set; } = "x-workflow-instance-id";
    public string CorrelationHeaderKey { get; set; } = "x-correlation-id";
}
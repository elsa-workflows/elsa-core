namespace Elsa.Kafka;

public interface IConsumerFactory
{
    IConsumer CreateConsumer(CreateConsumerContext workerContext);
}
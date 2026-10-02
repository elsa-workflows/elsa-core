namespace Elsa.ServiceBus.Kafka;

public interface IConsumerFactory
{
    IConsumer CreateConsumer(CreateConsumerContext workerContext);
}
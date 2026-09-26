namespace Elsa.ServiceBus.Kafka;

public interface IProducerFactory
{
    IProducer CreateProducer(CreateProducerContext workerContext);
}
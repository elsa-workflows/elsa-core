namespace Elsa.Kafka;

public interface IProducerFactory
{
    IProducer CreateProducer(CreateProducerContext workerContext);
}
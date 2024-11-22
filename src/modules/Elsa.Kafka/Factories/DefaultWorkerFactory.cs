using Confluent.Kafka;
using Elsa.Kafka.Implementations;

namespace Elsa.Kafka.Factories;

public class DefaultWorkerFactory : IWorkerFactory
{
    public IWorker CreateWorker(WorkerContext workerContext)
    {
        var consumerDefinition = workerContext.ConsumerDefinition;
        var config = consumerDefinition.Config;
        var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        return new Worker<Ignore, string>(workerContext, consumer);
    }
}
namespace Elsa.Kafka;

public interface IWorkerFactory
{
    IWorker CreateWorker(WorkerContext workerContext);
}
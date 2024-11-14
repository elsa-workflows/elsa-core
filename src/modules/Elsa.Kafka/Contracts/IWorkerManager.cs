namespace Elsa.Kafka;

public interface IWorkerManager
{
    Task UpdateWorkersAsync(CancellationToken cancellationToken = default);
    void StopWorkers();
    IWorker GetWorker(string consumerDefinitionId);
}
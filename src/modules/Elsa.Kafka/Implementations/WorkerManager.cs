using Confluent.Kafka;
using Elsa.Kafka.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka.Implementations;

public class WorkerManager(IConsumerDefinitionEnumerator consumerDefinitionEnumerator, IHasher hasher, IMediator mediator) : IWorkerManager
{
    private IDictionary<string, Worker> Workers { get; set; } = new Dictionary<string, Worker>();

    public async Task UpdateWorkersAsync(CancellationToken cancellationToken = default)
    {
        var consumerDefinitions = await consumerDefinitionEnumerator.EnumerateAsync(cancellationToken).ToList();
        var workers = new Dictionary<string, Worker>(Workers);
        var workersToRemove = workers.Keys.Except(consumerDefinitions.Select(x => x.Id)).ToList();

        // Remove workers that are no longer needed.
        foreach (var workerToRemove in workersToRemove)
        {
            if (workers.TryGetValue(workerToRemove, out var worker))
            {
                worker.Stop();
                workers.Remove(workerToRemove);
            }
        }

        // Add or update workers.
        foreach (var consumerDefinition in consumerDefinitions)
        {
            var existingWorker = workers.GetValueOrDefault(consumerDefinition.Id);

            if (existingWorker == null)
            {
                // Add a new worker.
                var worker = CreateWorker(consumerDefinition);
                workers.Add(consumerDefinition.Id, worker);
            }
            else
            {
                // Compare the existing worker's consumer definition with the new consumer definition using a hash.
                var existingHash = ComputeHash(existingWorker.ConsumerDefinition);
                var hash = ComputeHash(consumerDefinition);

                // If the hash is different, update the worker.
                if (existingHash != hash)
                {
                    existingWorker.Stop();
                    var worker = CreateWorker(consumerDefinition);
                    workers[consumerDefinition.Id] = worker;
                }
            }
        }

        // Ensure all workers are running.
        foreach (var worker in workers.Values)
            worker.Start(cancellationToken);

        // Update the workers dictionary.
        Workers = workers;
    }
    
    public void StopWorkers()
    {
        foreach (var worker in Workers.Values)
            worker.Stop();
    }

    public IWorker GetWorker(string consumerDefinitionId)
    {
        return Workers[consumerDefinitionId];
    }

    private Worker CreateWorker(ConsumerDefinition consumerDefinition)
    {
        var worker = new Worker(consumerDefinition);
        worker.MessageReceived = OnMessageReceivedAsync;
        return worker;
    }
    
    private async Task OnMessageReceivedAsync(Worker worker, Message<Ignore, string> arg, CancellationToken cancellationToken)
    {
        var headers = arg.Headers.ToDictionary(x => x.Key, x => x.GetValueBytes());
        var notification = new TransportMessageReceived(worker, new KafkaTransportMessage(arg.Key, arg.Value, headers));
        await mediator.SendAsync(notification, cancellationToken);
    }

    private string ComputeHash(ConsumerDefinition consumerDefinition)
    {
        return hasher.Hash(consumerDefinition);
    }
}
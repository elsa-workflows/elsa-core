using Elsa.AzureServiceBus.Services;

namespace Elsa.AzureServiceBus.Contracts;

/// <summary>
/// Manages message workers.
/// </summary>
public interface IWorkerManager
{
    /// <summary>
    /// A list of workers under management.
    /// </summary>
    IEnumerable<Worker> Workers { get; }

    /// <summary>
    /// Returns a value indicating whether a worker exists for the specified trigger.
    /// </summary>
    Worker? FindWorkerFor(string queueOrTopic, string? subscription);
    
    /// <summary>
    /// Creates a worker for the specified queue or topic and subscription.
    /// </summary>
    Task StartWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements the ref count for the worker for the specified queue or topic and subscription.
    /// If the worker's ref count reaches 0, it is removed.
    /// </summary>
    Task StopWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that at least one worker exists for the specified queue/topic and subscription.
    /// </summary>
    Task EnsureWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified worker.
    /// </summary>
    Task RemoveWorkerAsync(Worker worker);
}
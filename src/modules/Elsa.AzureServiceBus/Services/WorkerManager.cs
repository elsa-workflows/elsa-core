using Elsa.AzureServiceBus.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AzureServiceBus.Services;

/// <summary>
/// Manages message workers.
/// </summary>
public class WorkerManager : IWorkerManager, IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICollection<Worker> _workers = new List<Worker>();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkerManager"/> class.
    /// </summary>
    public WorkerManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// A list of workers under management.
    /// </summary>
    public IEnumerable<Worker> Workers => _workers.ToList();
    
    /// <inheritdoc />
    public Worker? FindWorkerFor(string queueOrTopic, string? subscription) => _workers.FirstOrDefault(x => x.QueueOrTopic == queueOrTopic && x.Subscription == subscription);

    /// <inheritdoc />
    public async Task StartWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        var worker = FindWorkerFor(queueOrTopic, subscription);

        if (worker != null)
        {
            worker.IncrementRefCount();
            return;
        }

        await CreateAndAddWorkerAsync(queueOrTopic, subscription, cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        var worker = FindWorkerFor(queueOrTopic, subscription);

        if (worker == null)
            return;

        worker.DecrementRefCount();

        if (worker.RefCount == 0) await RemoveWorkerAsync(worker);
    }

    /// <inheritdoc />
    public async Task EnsureWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        var worker = FindWorkerFor(queueOrTopic, subscription);
        if (worker != null) return;
        await CreateAndAddWorkerAsync(queueOrTopic, subscription, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveWorkerAsync(Worker worker)
    {
        _workers.Remove(worker);
        await worker.DisposeAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var worker in Workers) await worker.DisposeAsync();
    }
    
    private async Task CreateAndAddWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        subscription ??= "";
        var worker = ActivatorUtilities.CreateInstance<Worker>(_serviceProvider, queueOrTopic, subscription!);
        
        _workers.Add(worker);
        await worker.StartAsync(cancellationToken);
    }
}
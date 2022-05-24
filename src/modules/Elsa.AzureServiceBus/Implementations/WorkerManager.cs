using Elsa.AzureServiceBus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AzureServiceBus.Implementations;

public class WorkerManager : IWorkerManager, IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICollection<Worker> _workers = new List<Worker>();

    public WorkerManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyCollection<Worker> Workers => _workers.ToList();
    public Worker? FindWorkerFor(string queueOrTopic, string? subscription) => _workers.FirstOrDefault(x => x.QueueOrTopic == queueOrTopic && x.Subscription == subscription);

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

    public async Task StopWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        var worker = FindWorkerFor(queueOrTopic, subscription);

        if (worker == null)
            return;

        worker.DecrementRefCount();

        if (worker.RefCount == 0) await RemoveWorkerAsync(worker);
    }
    
    public async Task EnsureWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        var worker = FindWorkerFor(queueOrTopic, subscription);
        if (worker != null) return;
        await CreateAndAddWorkerAsync(queueOrTopic, subscription, cancellationToken);
    }

    public async Task RemoveWorkerAsync(Worker worker)
    {
        _workers.Remove(worker);
        await worker.DisposeAsync();
    }

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
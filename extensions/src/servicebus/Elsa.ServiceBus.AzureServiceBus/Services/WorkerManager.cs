using System.Collections.Concurrent;
using Elsa.ServiceBus.AzureServiceBus.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.AzureServiceBus.Services;

/// <summary>
/// Manages message workers.
/// </summary>
public class WorkerManager(IServiceProvider serviceProvider) : IWorkerManager, IAsyncDisposable
{
    private readonly ConcurrentBag<Worker> _workers = new();
    private readonly SemaphoreSlim _startLock = new(1, 1);

    /// <summary>
    /// A list of workers under management.
    /// </summary>
    public IEnumerable<Worker> Workers => _workers.ToList();

    /// <inheritdoc />
    public async Task StartWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        if (FindWorkerFor(queueOrTopic, subscription) != null) return;

        await _startLock.WaitAsync(cancellationToken);

        try
        {
            if (FindWorkerFor(queueOrTopic, subscription) != null) return;
            await CreateWorkerAsync(queueOrTopic, subscription, cancellationToken);
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <inheritdoc />
    public Worker? FindWorkerFor(string queueOrTopic, string? subscription) => _workers.FirstOrDefault(x => x.QueueOrTopic == queueOrTopic && x.Subscription == subscription);

    private async Task CreateWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        subscription ??= "";
        var worker = ActivatorUtilities.CreateInstance<Worker>(serviceProvider, queueOrTopic, subscription!);

        _workers.Add(worker);
        await worker.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var worker in Workers) await worker.DisposeAsync();
    }
}
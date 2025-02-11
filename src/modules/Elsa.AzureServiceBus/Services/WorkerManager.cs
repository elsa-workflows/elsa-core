using Elsa.AzureServiceBus.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AzureServiceBus.Services;

/// <summary>
/// Manages message workers.
/// </summary>
public class WorkerManager(IServiceProvider serviceProvider) : IWorkerManager, IAsyncDisposable
{
    private readonly ICollection<Worker> _workers = new List<Worker>();

    /// <summary>
    /// A list of workers under management.
    /// </summary>
    public IEnumerable<Worker> Workers => _workers.ToList();

    /// <inheritdoc />
    public async Task StartWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        var worker = FindWorkerFor(queueOrTopic, subscription);
        if (worker != null) return;
        await CreateWorkerAsync(queueOrTopic, subscription, cancellationToken);
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
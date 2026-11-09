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
    private TaskCompletionSource? _disposeCompletion;

    /// <summary>
    /// A list of workers under management.
    /// </summary>
    public IEnumerable<Worker> Workers => _workers.ToArray();

    /// <inheritdoc />
    public async Task StartWorkerAsync(string queueOrTopic, string? subscription, CancellationToken cancellationToken = default)
    {
        await _startLock.WaitAsync(cancellationToken);

        try
        {
            ObjectDisposedException.ThrowIf(_disposeCompletion != null, this);
            if (FindWorkerFor(queueOrTopic, subscription) != null)
            {
                return;
            }
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

        try
        {
            await worker.StartAsync(cancellationToken);
        }
        catch
        {
            await worker.DisposeAsync();
            throw;
        }

        _workers.Add(worker);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Worker[]? workers = null;
        await _startLock.WaitAsync();

        try
        {
            if (_disposeCompletion == null)
            {
                _disposeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    workers = Workers.ToArray();
                }
                catch (Exception exception)
                {
                    _disposeCompletion.SetException(exception);
                }
            }

            completion = _disposeCompletion;
        }
        finally
        {
            _startLock.Release();
        }

        // Processor shutdown can wait for handlers that request starts. Never
        // call worker code while holding the start gate.
        if (workers != null)
        {
            try
            {
                var failures = new List<Exception>();
                foreach (var worker in workers)
                {
                    try
                    {
                        await worker.DisposeAsync();
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }

                if (failures.Count == 0)
                {
                    completion.SetResult();
                }
                else
                {
                    completion.SetException(failures.Count == 1 ? failures[0] : new AggregateException(failures));
                }
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        await completion.Task;
    }
}
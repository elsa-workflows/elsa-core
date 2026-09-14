using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.ComponentTests.Services;

/// <summary>
/// A test-specific bookmark queue worker that processes items immediately without throttling.
/// </summary>
public sealed class TestBookmarkQueueWorker(
    IBookmarkQueueSignaler signaler,
    IServiceScopeFactory scopeFactory,
    ILogger<TestBookmarkQueueWorker> logger) : IBookmarkQueueWorker, IAsyncDisposable
{
    private readonly object _lifetimeLock = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;
    private bool _disposed;

    public void Start()
    {
        lock (_lifetimeLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_workerTask is not null)
                return;

            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationTokenSource;
            _workerTask = Task.Run(() => AwaitSignalAsync(cancellationTokenSource.Token));
        }
    }

    public void Stop()
    {
        lock (_lifetimeLock)
        {
            if (_disposed)
                return;

            _cancellationTokenSource?.Cancel();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task? workerTask;
        CancellationTokenSource? cancellationTokenSource;

        lock (_lifetimeLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            workerTask = _workerTask;
            cancellationTokenSource = _cancellationTokenSource;
            _workerTask = null;
            _cancellationTokenSource = null;
        }

        List<Exception>? failures = null;

        try
        {
            cancellationTokenSource?.Cancel();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        try
        {
            if (workerTask is not null)
                await workerTask;
        }
        catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested == true)
        {
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }
        finally
        {
            try
            {
                cancellationTokenSource?.Dispose();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("Failed to stop the test bookmark queue worker cleanly.", failures);
    }

    private async Task AwaitSignalAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await signaler.AwaitAsync(cancellationToken);
                await ProcessAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Test bookmark queue worker failed; continuing the loop.");
            }
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Processing bookmark queue without throttling.");
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>();
        await processor.ProcessAsync(cancellationToken);
        logger.LogDebug("Processed bookmark queue.");
    }
}

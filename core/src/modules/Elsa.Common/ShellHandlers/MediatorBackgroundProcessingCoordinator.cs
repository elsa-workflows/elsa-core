using Elsa.Mediator.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Common.ShellHandlers;

public sealed class MediatorBackgroundProcessingCoordinator(
    BackgroundCommandProcessor commandProcessor,
    BackgroundNotificationProcessor notificationProcessor,
    BackgroundJobProcessor jobProcessor,
    ILogger<MediatorBackgroundProcessingCoordinator> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;
    private int _referenceCount;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_referenceCount++ > 0)
                return;

            _cancellationTokenSource = new();
            _processingTask = Task.WhenAll(
                commandProcessor.ExecuteAsync(_cancellationTokenSource.Token),
                notificationProcessor.ExecuteAsync(_cancellationTokenSource.Token),
                jobProcessor.ExecuteAsync(_cancellationTokenSource.Token));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? processingTask;
        CancellationTokenSource? cancellationTokenSource;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_referenceCount == 0)
                return;

            if (--_referenceCount > 0)
                return;

            processingTask = _processingTask;
            cancellationTokenSource = _cancellationTokenSource;
            _processingTask = null;
            _cancellationTokenSource = null;
        }
        finally
        {
            _lock.Release();
        }

        if (cancellationTokenSource is null)
            return;

        await cancellationTokenSource.CancelAsync();

        if (processingTask is not null)
        {
            try
            {
                await processingTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occurred while stopping mediator background processing");
                throw;
            }
        }

        cancellationTokenSource.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _referenceCount = 1;
        await StopAsync(CancellationToken.None);
        _lock.Dispose();
    }
}

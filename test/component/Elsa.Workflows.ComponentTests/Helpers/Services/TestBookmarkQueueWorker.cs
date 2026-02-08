using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.ComponentTests.Services;

/// <summary>
/// A test-specific bookmark queue worker that processes items immediately without throttling.
/// This prevents timeouts in tests where many workflows complete rapidly.
/// </summary>
public class TestBookmarkQueueWorker(IBookmarkQueueSignaler signaler, IServiceScopeFactory scopeFactory, ILogger<TestBookmarkQueueWorker> logger) : IBookmarkQueueWorker
{
    private CancellationTokenSource _cts = null!;
    private bool _running;

    public void Start()
    {
        if (_running)
            return;

        _cts = new();
        _running = true;

        _ = Task.Run(AwaitSignalAsync);
    }

    public void Stop()
    {
        if (_running)
        {
            _running = false;
            _cts.Cancel();
        }

        _cts.Dispose();
    }

    private async Task AwaitSignalAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await signaler.AwaitAsync(_cts.Token);
                // Process immediately without throttling for tests
                await ProcessAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break; // Stop() was called
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TestBookmarkQueueWorker error – continuing loop");
            }
        }
    }

    protected virtual async Task ProcessAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Processing bookmark queue (test mode - no throttling)...");
        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>();
        await processor.ProcessAsync(cancellationToken);
        logger.LogDebug("Processed bookmark queue.");
    }
}

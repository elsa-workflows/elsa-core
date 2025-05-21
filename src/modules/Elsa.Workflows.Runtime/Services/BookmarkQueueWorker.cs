using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThrottleDebounce;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueWorker : IBookmarkQueueWorker
{
    private readonly RateLimitedFunc<CancellationToken, Task> _rateLimitedProcessAsync;
    private CancellationTokenSource _cts = null!;
    private bool _running;
    private readonly IBookmarkQueueSignaler _signaler;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookmarkQueueWorker> _logger;
    
    public BookmarkQueueWorker(IBookmarkQueueSignaler signaler, IServiceScopeFactory scopeFactory, ILogger<BookmarkQueueWorker> logger)
    {
        _signaler = signaler;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _rateLimitedProcessAsync = Throttler.Throttle<CancellationToken, Task>(ProcessAsync, TimeSpan.FromMilliseconds(500));
    }

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
                await _signaler.AwaitAsync(_cts.Token);
                await _rateLimitedProcessAsync.InvokeAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break; // Stop() was called
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookmarkQueueWorker error – continuing loop");
            }
        }
    }

    protected virtual async Task ProcessAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing bookmark queue...");
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>();
        await processor.ProcessAsync(cancellationToken);
        _logger.LogDebug("Processed bookmark queue.");
    }
}
using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThrottleDebounce;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueWorker : IBookmarkQueueWorker
{
    private readonly RateLimitedFunc<CancellationToken, Task>? _rateLimitedProcessAsync;
    private CancellationTokenSource _cts = null!;
    private bool _running;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITenantScopeFactory? _tenantScopeFactory;
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly ILogger<BookmarkQueueWorker> _logger;
    private Tenant? _tenant;
    protected IBookmarkQueueSignaler Signaler { get; }
    protected internal string CapturedTenantId => (_tenant?.Id).NormalizeTenantId();

    public BookmarkQueueWorker(
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        ILogger<BookmarkQueueWorker> logger,
        ITenantScopeFactory? tenantScopeFactory = null,
        ITenantAccessor? tenantAccessor = null)
        : this(signaler, scopeFactory, logger, tenantScopeFactory, tenantAccessor, TimeSpan.FromMilliseconds(500))
    {
    }

    protected BookmarkQueueWorker(
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        ILogger<BookmarkQueueWorker> logger,
        ITenantScopeFactory? tenantScopeFactory,
        ITenantAccessor? tenantAccessor,
        TimeSpan processThrottle)
    {
        Signaler = signaler;
        _scopeFactory = scopeFactory;
        _tenantScopeFactory = tenantScopeFactory;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
        _tenant = tenantAccessor?.Tenant;
        if (processThrottle > TimeSpan.Zero)
            _rateLimitedProcessAsync = Throttler.Throttle<CancellationToken, Task>(ProcessAsync, processThrottle);
    }

    public void Start()
    {
        if (_running)
            return;

        _tenant ??= _tenantAccessor?.Tenant;
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
            // Release is on the concrete type so IBookmarkQueueSignaler stays unchanged; a decorated signaler is left as-is.
            if (Signaler is BookmarkQueueSignaler bookmarkQueueSignaler)
                bookmarkQueueSignaler.Release(CapturedTenantId);
        }

        _cts.Dispose();
    }

    private async Task AwaitSignalAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var tenantContext = _tenantAccessor?.PushContext(_tenant);
                await Signaler.AwaitAsync(_cts.Token);
                await InvokeProcessAsync(_cts.Token);
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

    private Task InvokeProcessAsync(CancellationToken cancellationToken)
    {
        return _rateLimitedProcessAsync is not null
            ? _rateLimitedProcessAsync.InvokeAsync(cancellationToken)
            : ProcessAsync(cancellationToken);
    }

    protected virtual async Task ProcessAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing bookmark queue...");

        if (_tenantScopeFactory is not null && _tenant is not null)
        {
            await using var tenantScope = _tenantScopeFactory.CreateScope(_tenant);
            var processor = tenantScope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>();
            await processor.ProcessAsync(cancellationToken);
        }
        else
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>();
            await processor.ProcessAsync(cancellationToken);
        }

        _logger.LogDebug("Processed bookmark queue.");
    }
}

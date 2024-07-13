using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueWorker(IBookmarkQueueWorkerSignaler signaler, IServiceScopeFactory scopeFactory) : IBookmarkQueueWorker
{
    private bool _running;
    private CancellationTokenSource _cts = default!;
    
    public void Start()
    {
        if(_running)
            return;

        _cts = new();
        _running = true;
        
        _ = Task.Run(AwaitSignalAsync);
    }

    public void Stop()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task AwaitSignalAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            await signaler.AwaitAsync();
            await ProcessAsync(_cts.Token);
        }
    }
    
    protected virtual async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>();
        await processor.ProcessAsync(cancellationToken);
    }
}
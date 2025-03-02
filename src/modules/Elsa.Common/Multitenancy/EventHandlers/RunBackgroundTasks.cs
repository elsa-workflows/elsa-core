using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy.EventHandlers;

public class RunBackgroundTasks : ITenantActivatedEvent, ITenantDeactivatedEvent
{
    private readonly ICollection<Task> _runningTasks = new List<Task>();
    private CancellationTokenSource _cancellationTokenSource = default!;
    
    public Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        var cancellationToken = args.CancellationToken;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        var tenantScope = args.TenantScope;
        var backgroundTasks = tenantScope.ServiceProvider.GetServices<IBackgroundTask>();
        var backgroundTaskStarter = tenantScope.ServiceProvider.GetRequiredService<IBackgroundTaskStarter>();
        
        foreach (var backgroundTask in backgroundTasks)
        {
            var task = backgroundTaskStarter.StartAsync(backgroundTask, _cancellationTokenSource.Token);
            if (!task.IsCompleted) _runningTasks.Add(task);
        }

        return Task.CompletedTask;
    }

    public Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
    {
        _cancellationTokenSource.Cancel();
        _runningTasks.Clear();
        return Task.CompletedTask;
    }
}
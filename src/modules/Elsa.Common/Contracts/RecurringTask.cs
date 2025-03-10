namespace Elsa.Common;

public abstract class RecurringTask : IRecurringTask
{
    public virtual Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
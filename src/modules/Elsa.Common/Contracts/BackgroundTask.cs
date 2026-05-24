namespace Elsa.Common;

public abstract class BackgroundTask : IBackgroundTask
{
    public abstract Task ExecuteAsync(CancellationToken cancellationToken);
    public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

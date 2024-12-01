namespace Elsa.Common.Multitenancy;

public interface IBackgroundTaskStarter
{
    Task StartAsync(IBackgroundTask task, CancellationToken cancellationToken);
    Task StopAsync(IBackgroundTask task, CancellationToken cancellationToken);
}
namespace Elsa.Common;

/// <summary>
/// Represents a task that is executed in the background.
/// In multi-tenant applications, this task is executed for each tenant.
/// </summary>
public interface IBackgroundTask : ITask
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
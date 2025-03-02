namespace Elsa.Common;

/// <summary>
/// Represents a recurring task that is executed in the background.
/// In multi-tenant applications, this task is executed for each tenant.
/// </summary>
public interface IRecurringTask : ITask
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
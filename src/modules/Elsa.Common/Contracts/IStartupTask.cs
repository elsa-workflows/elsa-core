namespace Elsa.Common;

/// <summary>
/// Represents a task that is executed during application startup.
/// In multi-tenant applications, this task is executed for each tenant.
/// </summary>
public interface IStartupTask : ITask
{
}
namespace Elsa.Common.Multitenancy;

public interface ITaskExecutor
{
    Task ExecuteTaskAsync(ITask task, CancellationToken cancellationToken);
}
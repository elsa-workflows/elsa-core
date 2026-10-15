namespace Elsa.Common;

public interface ITask
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
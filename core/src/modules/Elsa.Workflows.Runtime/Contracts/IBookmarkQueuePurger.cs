namespace Elsa.Workflows.Runtime;

public interface IBookmarkQueuePurger
{
    Task PurgeAsync(CancellationToken cancellationToken = default);
}
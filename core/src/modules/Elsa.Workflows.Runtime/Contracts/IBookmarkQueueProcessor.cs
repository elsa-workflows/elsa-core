namespace Elsa.Workflows.Runtime;

public interface IBookmarkQueueProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
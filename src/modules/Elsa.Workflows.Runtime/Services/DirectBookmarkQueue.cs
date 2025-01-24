namespace Elsa.Workflows.Runtime;

public class DirectBookmarkQueue(IBookmarkResumer resumer) : IBookmarkQueue
{
    public async Task EnqueueAsync(NewBookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var filter = item.CreateBookmarkFilter();
        var options = item.Options;
        await resumer.ResumeAsync(filter, options, cancellationToken);
    }
}
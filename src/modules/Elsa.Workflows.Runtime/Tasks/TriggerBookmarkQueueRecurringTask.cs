using Elsa.Common;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Periodically signals the bookmark queue processor to check for new items. This is a reliability measure that ensures stimuli never gets missed.
/// </summary>
[UsedImplicitly]
public class TriggerBookmarkQueueRecurringTask(IBookmarkQueueWorker bookmarkQueueWorker, IBookmarkQueueSignaler signaler) : IRecurringTask
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        bookmarkQueueWorker.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        bookmarkQueueWorker.Stop();
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return signaler.TriggerAsync(stoppingToken);
    }
}
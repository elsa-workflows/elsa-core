using Elsa.Scheduling.Bookmarks;
using Elsa.Scheduling.Services;
using Elsa.Workflows.Runtime.Entities;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.Services;

public class DefaultBookmarkSchedulerTests
{
    private readonly IWorkflowScheduler _workflowScheduler;
    private readonly DefaultBookmarkScheduler _scheduler;

    public DefaultBookmarkSchedulerTests()
    {
        _workflowScheduler = Substitute.For<IWorkflowScheduler>();
        _scheduler = new DefaultBookmarkScheduler(_workflowScheduler);
    }

    [Fact]
    public async Task ScheduleAsync_WithStoredBookmarks_StartsSchedulingConcurrently()
    {
        var releaseScheduler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scheduledCount = 0;
        var bookmarks = CreateStoredDelayBookmarks(2);
        _workflowScheduler
            .ScheduleAtAsync(Arg.Any<string>(), Arg.Any<ScheduleExistingWorkflowInstanceRequest>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref scheduledCount);
                return new ValueTask(releaseScheduler.Task);
            });

        var scheduleTask = _scheduler.ScheduleAsync(bookmarks, CancellationToken.None);

        var allSchedulingStarted = await WaitUntilAsync(() => Volatile.Read(ref scheduledCount) == bookmarks.Count, TimeSpan.FromSeconds(5));
        var completedBeforeRelease = scheduleTask.IsCompleted;
        releaseScheduler.SetResult();
        await scheduleTask;

        Assert.True(allSchedulingStarted);
        Assert.False(completedBeforeRelease);
    }

    private static IReadOnlyCollection<StoredBookmark> CreateStoredDelayBookmarks(int count)
    {
        var now = DateTimeOffset.UtcNow;
        return Enumerable.Range(1, count).Select(index => new StoredBookmark
        {
            Id = $"bookmark-{index}",
            Name = SchedulingStimulusNames.Delay,
            Hash = $"hash-{index}",
            WorkflowInstanceId = $"workflow-{index}",
            Payload = new DelayPayload(now.AddMinutes(index)),
            CreatedAt = now
        }).ToList();
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopAt = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < stopAt)
        {
            if (condition())
                return true;

            await Task.Delay(10);
        }

        return condition();
    }
}

using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.Services;

/// <summary>
/// Covers <see cref="IActivityScheduler.RemoveWhere"/>, the operation a container uses to withdraw work it scheduled
/// but has since decided must not run. What survives has to come back in the order it would have been taken in, which
/// is the part a naive "clear and re-add" gets wrong for the LIFO scheduler.
/// </summary>
public class ActivitySchedulerTests
{
    public static TheoryData<Func<IActivityScheduler>> Schedulers => new()
    {
        () => new QueueBasedActivityScheduler(),
        () => new StackBasedActivityScheduler()
    };

    [Theory]
    [MemberData(nameof(Schedulers))]
    public void RemoveWhere_RemovesOnlyMatchingItems_AndReportsHowMany(Func<IActivityScheduler> createScheduler)
    {
        var scheduler = Schedule(createScheduler(), "a", "b", "c", "d");

        var removedCount = scheduler.RemoveWhere(x => x.Activity.Id is "b" or "d");

        Assert.Equal(2, removedCount);

        // The survivors come out in the same order as a scheduler that only ever held them.
        Assert.Equal(TakeAll(Schedule(createScheduler(), "a", "c")), TakeAll(scheduler));
    }

    [Theory]
    [MemberData(nameof(Schedulers))]
    public void RemoveWhere_LeavesTheSchedulerUntouched_WhenNothingMatches(Func<IActivityScheduler> createScheduler)
    {
        var scheduler = Schedule(createScheduler(), "a", "b", "c");

        var removedCount = scheduler.RemoveWhere(_ => false);

        Assert.Equal(0, removedCount);
        Assert.Equal(TakeAll(Schedule(createScheduler(), "a", "b", "c")), TakeAll(scheduler));
    }

    [Theory]
    [MemberData(nameof(Schedulers))]
    public void RemoveWhere_EmptiesTheScheduler_WhenEverythingMatches(Func<IActivityScheduler> createScheduler)
    {
        var scheduler = Schedule(createScheduler(), "a", "b", "c");

        var removedCount = scheduler.RemoveWhere(_ => true);

        Assert.Equal(3, removedCount);
        Assert.False(scheduler.HasAny);
    }

    private static IActivityScheduler Schedule(IActivityScheduler scheduler, params string[] activityIds)
    {
        foreach (var activityId in activityIds)
            scheduler.Schedule(new ActivityWorkItem(new WriteLine(activityId) { Id = activityId }));

        return scheduler;
    }

    /// <summary>Drains the scheduler, so assertions are about the order the work would actually have been taken in.</summary>
    private static List<string> TakeAll(IActivityScheduler scheduler)
    {
        var activityIds = new List<string>();

        while (scheduler.HasAny)
            activityIds.Add(scheduler.Take().Activity.Id);

        return activityIds;
    }
}

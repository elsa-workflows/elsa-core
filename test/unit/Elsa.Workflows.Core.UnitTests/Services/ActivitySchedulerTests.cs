using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Services;

/// <summary>
/// Covers <see cref="IActivityScheduler.RemoveWhere"/>, the operation a container uses to withdraw work it scheduled
/// but has since decided must not run. What survives has to come back in the order it would have been taken in, which
/// is the part a naive "clear and re-add" gets wrong for the LIFO scheduler.
/// </summary>
public class ActivitySchedulerTests
{
    public sealed record SchedulerFactory(Func<IActivityScheduler> Create);

    public static IEnumerable<Func<SchedulerFactory>> Schedulers()
    {
        yield return () => new(() => new QueueBasedActivityScheduler());
        yield return () => new(() => new StackBasedActivityScheduler());
    }

    [Test]
    [MethodDataSource(nameof(Schedulers))]
    public async Task RemoveWhere_RemovesOnlyMatchingItems_AndReportsHowMany(SchedulerFactory schedulerFactory)
    {
        var scheduler = Schedule(schedulerFactory.Create(), "a", "b", "c", "d");

        var removedCount = scheduler.RemoveWhere(x => x.Activity.Id is "b" or "d");

        await Assert.That(removedCount).IsEqualTo(2);

        // The survivors come out in the same order as a scheduler that only ever held them.
        await Assert.That(TakeAll(scheduler)).IsEquivalentTo(
            TakeAll(Schedule(schedulerFactory.Create(), "a", "c")),
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [MethodDataSource(nameof(Schedulers))]
    public async Task RemoveWhere_LeavesTheSchedulerUntouched_WhenNothingMatches(SchedulerFactory schedulerFactory)
    {
        var scheduler = Schedule(schedulerFactory.Create(), "a", "b", "c");

        var removedCount = scheduler.RemoveWhere(_ => false);

        await Assert.That(removedCount).IsEqualTo(0);
        await Assert.That(TakeAll(scheduler)).IsEquivalentTo(
            TakeAll(Schedule(schedulerFactory.Create(), "a", "b", "c")),
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [MethodDataSource(nameof(Schedulers))]
    public async Task RemoveWhere_EmptiesTheScheduler_WhenEverythingMatches(SchedulerFactory schedulerFactory)
    {
        var scheduler = Schedule(schedulerFactory.Create(), "a", "b", "c");

        var removedCount = scheduler.RemoveWhere(_ => true);

        await Assert.That(removedCount).IsEqualTo(3);
        await Assert.That(scheduler.HasAny).IsFalse();
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

using Elsa.Testing.Shared;
using Elsa.Workflows;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Looping;

public class ParallelForEachTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task Should_Schedule_Body_For_Each_Item(int itemCount)
    {
        var items = Enumerable.Range(0, itemCount).Select(i => $"item{i}").ToArray();
        var body = new MockBodyActivity();
        var parallelForEach = new ParallelForEach<string>(items) { Body = body };

        var context = await ExecuteAsync(parallelForEach);

        await AssertScheduledCount(context, itemCount);
        await Assert.That(context.HasScheduledActivity(body)).IsTrue();
    }

    [Test]
    public async Task Should_Complete_When_Items_Empty()
    {
        var body = new MockBodyActivity();
        var parallelForEach = new ParallelForEach<string>(Array.Empty<string>()) { Body = body };

        var context = await ExecuteAsync(parallelForEach);

        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(context.HasScheduledActivity(body)).IsFalse();
    }

    [Test]
    public async Task Should_Not_Schedule_When_Body_Null()
    {
        var items = new[] { "a", "b" };
        var parallelForEach = new ParallelForEach<string>(items) { Body = null! };

        var context = await ExecuteAsync(parallelForEach);

        await AssertScheduledCount(context, 0);
    }

    [Test]
    public async Task Should_Handle_Integer_Items()
    {
        var items = new[] { 1, 2, 3 };
        var body = new MockBodyActivity();
        var parallelForEach = new ParallelForEach<int>(items) { Body = body };

        var context = await ExecuteAsync(parallelForEach);

        await AssertScheduledCount(context, items.Length);
    }

    [Test]
    public void Verify_Activity_Attributes()
    {
        var parallelForEach = new ParallelForEach();
        var fixture = new ActivityTestFixture(parallelForEach);

        fixture.AssertActivityAttributes(
            expectedNamespace: "Elsa",
            expectedKind: ActivityKind.Action,
            expectedCategory: "Looping",
            expectedDescription: "Schedule an activity for each item in parallel."
        );
    }

    [Test]
    public async Task Default_Property_Values()
    {
        var parallelForEach = new ParallelForEach<string>();

        await Assert.That(parallelForEach.Items).IsNotNull();
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity) =>
        new ActivityTestFixture(activity).ExecuteAsync();

    private static async Task AssertScheduledCount(ActivityExecutionContext context, int expectedCount)
    {
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities.Count).IsEqualTo(expectedCount);
    }

    private class MockBodyActivity : Activity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;
    }
}

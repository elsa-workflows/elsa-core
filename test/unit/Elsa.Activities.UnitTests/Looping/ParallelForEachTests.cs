using Elsa.Testing.Shared;
using Elsa.Workflows;

namespace Elsa.Activities.UnitTests.Looping;

public class ParallelForEachTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Should_Schedule_Body_For_Each_Item(int itemCount)
    {
        var items = Enumerable.Range(0, itemCount).Select(i => $"item{i}").ToArray();
        var body = new MockBodyActivity();
        var parallelForEach = new ParallelForEach<string>(items) { Body = body };

        var context = await ExecuteAsync(parallelForEach);

        AssertScheduledCount(context, itemCount);
        Assert.True(context.HasScheduledActivity(body));
    }

    [Fact]
    public async Task Should_Complete_When_Items_Empty()
    {
        var body = new MockBodyActivity();
        var parallelForEach = new ParallelForEach<string>(Array.Empty<string>()) { Body = body };

        var context = await ExecuteAsync(parallelForEach);

        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.False(context.HasScheduledActivity(body));
    }

    [Fact]
    public async Task Should_Not_Schedule_When_Body_Null()
    {
        var items = new[] { "a", "b" };
        var parallelForEach = new ParallelForEach<string>(items) { Body = null! };

        var context = await ExecuteAsync(parallelForEach);

        AssertScheduledCount(context, 0);
    }

    [Fact]
    public async Task Should_Handle_Integer_Items()
    {
        var items = new[] { 1, 2, 3 };
        var body = new MockBodyActivity();
        var parallelForEach = new ParallelForEach<int>(items) { Body = body };

        var context = await ExecuteAsync(parallelForEach);

        AssertScheduledCount(context, items.Length);
    }

    [Fact]
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

    [Fact]
    public void Default_Property_Values()
    {
        var parallelForEach = new ParallelForEach<string>();

        Assert.NotNull(parallelForEach.Items);
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity) =>
        new ActivityTestFixture(activity).ExecuteAsync();

    private static void AssertScheduledCount(ActivityExecutionContext context, int expectedCount)
    {
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Equal(expectedCount, scheduledActivities.Count);
    }

    private class MockBodyActivity : Activity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;
    }
}

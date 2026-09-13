using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Parallel;

public class ParallelTests
{
    [Test]
    [DisplayName("Parallel should schedule multiple activities")]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    public async Task Should_Schedule_Multiple_Activities(int activityCount)
    {
        // Arrange
        var activities = Enumerable.Range(1, activityCount)
            .Select(i => new WriteLine($"Activity {i}"))
            .Cast<IActivity>()
            .ToArray();

        var parallel = new Workflows.Activities.Parallel(activities);

        // Act
        var context = await new ActivityTestFixture(parallel).ExecuteAsync();

        // Assert - All activities should be scheduled
        foreach (var activity in activities)
        {
            await Assert.That(context.HasScheduledActivity(activity)).IsTrue();
        }
    }

    [Test]
    [DisplayName("Parallel should not schedule activities when empty")]
    public async Task Should_Not_Schedule_Activities_When_Empty()
    {
        // Arrange
        var parallel = new Workflows.Activities.Parallel();

        // Act
        var context = await new ActivityTestFixture(parallel).ExecuteAsync();

        // Assert - No activities should be scheduled (completion is tested in integration tests)
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities).IsEmpty();
    }

    [Test]
    [DisplayName("Parallel should schedule a single activity")]
    public async Task Should_Execute_Single_Activity()
    {
        // Arrange
        var activity = Substitute.For<IActivity>();
        var parallel = new Workflows.Activities.Parallel(activity);

        // Act
        var context = await new ActivityTestFixture(parallel).ExecuteAsync();

        // Assert
        await Assert.That(context.HasScheduledActivity(activity)).IsTrue();
    }

    [Test]
    [DisplayName("Parallel should schedule mixed activity types")]
    public async Task Should_Schedule_Mixed_Activity_Types()
    {
        // Arrange
        var writeLine = new WriteLine("Test output");
        var testVar = new Variable<int>("testVar", 0, "testVar");
        var setVariable = new SetVariable<int>(testVar, new Input<int>(42));
        var mockActivity = Substitute.For<IActivity>();

        var parallel = new Workflows.Activities.Parallel(writeLine, setVariable, mockActivity);

        // Act
        var context = await new ActivityTestFixture(parallel).ExecuteAsync();

        // Assert - All activities should be scheduled
        await Assert.That(context.HasScheduledActivity(writeLine)).IsTrue();
        await Assert.That(context.HasScheduledActivity(setVariable)).IsTrue();
        await Assert.That(context.HasScheduledActivity(mockActivity)).IsTrue();
    }

    [Test]
    [DisplayName("Parallel should track scheduled children count internally")]
    public async Task Should_Set_Scheduled_Children_Property()
    {
        // Arrange
        var activity1 = new WriteLine("Activity 1");
        var activity2 = new WriteLine("Activity 2");
        var activity3 = new WriteLine("Activity 3");
        var parallel = new Workflows.Activities.Parallel(activity1, activity2, activity3);

        // Act
        var context = await new ActivityTestFixture(parallel).ExecuteAsync();

        // Assert - The parallel activity should track that 3 children were scheduled
        var scheduledChildrenCount = context.GetProperty<int>("ScheduledChildren");
        await Assert.That(scheduledChildrenCount).IsEqualTo(3);
    }
}
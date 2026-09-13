using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the <see cref="Sequence"/> activity focusing on sequential scheduling behavior.
/// </summary>
public class SequenceTests
{
    [Test]
    [DisplayName("Sequence schedules first child activity")]
    [Arguments(1)]
    [Arguments(3)]
    [Arguments(5)]
    public async Task Sequence_SchedulesFirstChildActivity(int activityCount)
    {
        // Arrange
        var activities = CreateActivities(activityCount);
        var sequence = CreateSequenceWithActivities(activities);

        // Act
        var context = await ExecuteSequenceAsync(sequence);

        // Assert - Only first activity should be scheduled (sequential scheduling)
        await Assert.That(context.HasScheduledActivity(activities[0])).IsTrue();
    }

    [Test]
    [DisplayName("Sequence with no activities completes without scheduling")]
    public async Task Sequence_WithNoActivities_CompletesWithoutScheduling()
    {
        // Arrange
        var sequence = new Sequence();

        // Act
        var context = await ExecuteSequenceAsync(sequence);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities).IsEmpty();
    }

    [Test]
    [DisplayName("Sequence with single activity schedules it")]
    public async Task Sequence_WithSingleActivity_SchedulesIt()
    {
        // Arrange
        var activity = new WriteLine("Single activity");
        var sequence = CreateSequenceWithActivities(activity);

        // Act
        var context = await ExecuteSequenceAsync(sequence);

        // Assert
        await Assert.That(context.HasScheduledActivity(activity)).IsTrue();
    }

    [Test]
    [DisplayName("Sequence tracks current index property")]
    public async Task Sequence_TracksCurrentIndexProperty()
    {
        // Arrange
        var activities = CreateActivities(3);
        var sequence = CreateSequenceWithActivities(activities);

        // Act
        var context = await ExecuteSequenceAsync(sequence);

        // Assert - CurrentIndex should be incremented after scheduling first activity
        var currentIndex = context.GetProperty<int>("CurrentIndex");
        await Assert.That(currentIndex).IsEqualTo(1);
    }

    [Test]
    [DisplayName("Sequence schedules activities with different counts")]
    [Arguments(2)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task Sequence_SchedulesActivitiesWithDifferentCounts(int activityCount)
    {
        // Arrange
        var activities = CreateActivities(activityCount);
        var sequence = CreateSequenceWithActivities(activities);

        // Act
        var context = await ExecuteSequenceAsync(sequence);

        // Assert - Only first activity should be scheduled initially
        await Assert.That(context.HasScheduledActivity(activities[0])).IsTrue();
        if (activityCount > 1)
        {
            await Assert.That(context.HasScheduledActivity(activities[1])).IsFalse();
        }
    }

    [Test]
    [DisplayName("Sequence schedules mixed activity types")]
    public async Task Sequence_SchedulesMixedActivityTypes()
    {
        // Arrange
        var writeLine = new WriteLine("Test output");
        var testVar = new Variable<int>("testVar", 0, "testVar");
        var setVariable = new SetVariable<int>(testVar, new Input<int>(42));
        var mockActivity = Substitute.For<IActivity>();
        var sequence = CreateSequenceWithActivities(writeLine, setVariable, mockActivity);

        // Act
        var context = await ExecuteSequenceAsync(sequence);

        // Assert - First activity should be scheduled
        await Assert.That(context.HasScheduledActivity(writeLine)).IsTrue();
        await Assert.That(context.HasScheduledActivity(setVariable)).IsFalse();
        await Assert.That(context.HasScheduledActivity(mockActivity)).IsFalse();
    }

    [Test]
    [DisplayName("Sequence with variables declares them in memory")]
    public async Task Sequence_WithVariables_DeclaresThemInMemory()
    {
        // Arrange
        var variable1 = new Variable<int>("Counter", 10);
        var variable2 = new Variable<string>("Name", "Test");
        var sequence = new Sequence
        {
            Variables = { variable1, variable2 },
            Activities = { new WriteLine("Activity") }
        };

        // Act
        var context = await ExecuteSequenceAsync(sequence);

        // Assert
        var memory = context.ExpressionExecutionContext.Memory;
        await Assert.That(memory.HasBlock(variable1.Id)).IsTrue();
        await Assert.That(memory.HasBlock(variable2.Id)).IsTrue();
    }

    private static Task<ActivityExecutionContext> ExecuteSequenceAsync(Sequence sequence) =>
        new ActivityTestFixture(sequence).ExecuteAsync();

    private static IActivity[] CreateActivities(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new WriteLine($"Activity {i}"))
            .Cast<IActivity>()
            .ToArray();

    private static Sequence CreateSequenceWithActivities(params IActivity[] activities)
    {
        var sequence = new Sequence();
        foreach (var activity in activities)
        {
            sequence.Activities.Add(activity);
        }
        return sequence;
    }
}
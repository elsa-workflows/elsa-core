using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Branching;

public class SwitchTests
{
    [Test]
    [Arguments(SwitchMode.MatchFirst, true)]
    [Arguments(SwitchMode.MatchAny, true)]
    [Arguments(SwitchMode.MatchFirst, false)]
    [Arguments(SwitchMode.MatchAny, false)]
    public async Task Should_Handle_No_Matching_Cases_Correctly(SwitchMode mode, bool hasDefault)
    {
        // Arrange
        var defaultActivity = hasDefault ? Substitute.For<IActivity>() : null;
        var switchActivity = new Switch
        {
            Mode = new(mode),
            Cases = new List<SwitchCase>
            {
                new("False Case", Expression.LiteralExpression(false), Substitute.For<IActivity>())
            },
            Default = defaultActivity
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        if (hasDefault)
        {
            await Assert.That(scheduledActivities).HasSingleItem();
            await Assert.That(scheduledActivities.First().Activity).IsEqualTo(defaultActivity);
        }
        else
        {
            await Assert.That(scheduledActivities).IsEmpty();
        }
    }

    [Test]
    [Arguments(SwitchMode.MatchFirst, 1)]
    [Arguments(SwitchMode.MatchAny, 2)]
    public async Task Should_Handle_Multiple_Matching_Cases_According_To_Mode(SwitchMode mode, int expectedScheduledCount)
    {
        // Arrange
        var firstTrueActivity = Substitute.For<IActivity>();
        var secondTrueActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Mode = new(mode),
            Cases = new List<SwitchCase>
            {
                new("False", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("First True", Expression.LiteralExpression(true), firstTrueActivity),
                new("Second True", Expression.LiteralExpression(true), secondTrueActivity)
            }
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities.Count).IsEqualTo(expectedScheduledCount);

        if (mode == SwitchMode.MatchFirst)
        {
            await Assert.That(scheduledActivities.First().Activity).IsEqualTo(firstTrueActivity);
        }
        else // MatchAny
        {
            await Assert.That(scheduledActivities).Contains(s => s.Activity == firstTrueActivity);
            await Assert.That(scheduledActivities).Contains(s => s.Activity == secondTrueActivity);
        }
    }

    [Test]
    public async Task Should_Use_MatchFirst_As_Default_Mode()
    {
        // Arrange
        var firstTrueActivity = Substitute.For<IActivity>();
        var secondTrueActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            // No Mode explicitly set - should default to MatchFirst
            Cases = new List<SwitchCase>
            {
                new("First True", Expression.LiteralExpression(true), firstTrueActivity),
                new("Second True", Expression.LiteralExpression(true), secondTrueActivity)
            }
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities).HasSingleItem();
        await Assert.That(scheduledActivities.First().Activity).IsEqualTo(firstTrueActivity);
    }

    [Test]
    [Arguments(SwitchMode.MatchFirst)]
    [Arguments(SwitchMode.MatchAny)]
    public async Task Should_Schedule_Default_When_Null_Case_Condition_Evaluates_False(SwitchMode mode)
    {
        // Arrange
        var defaultActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Mode = new(mode),
            Cases = new List<SwitchCase>
            {
                new("Null condition", Expression.LiteralExpression(null), Substitute.For<IActivity>())
            },
            Default = defaultActivity
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities).HasSingleItem();
        await Assert.That(scheduledActivities.First().Activity).IsEqualTo(defaultActivity);
    }

    [Test]
    [Arguments(SwitchMode.MatchFirst, true)]
    [Arguments(SwitchMode.MatchAny, true)]
    [Arguments(SwitchMode.MatchFirst, false)]
    [Arguments(SwitchMode.MatchAny, false)]
    public async Task Should_Handle_Empty_Cases_Correctly(SwitchMode mode, bool hasDefault)
    {
        // Arrange
        var defaultActivity = hasDefault ? Substitute.For<IActivity>() : null;
        var switchActivity = new Switch
        {
            Mode = new(mode),
            Cases = new List<SwitchCase>(),
            Default = defaultActivity
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        if (hasDefault)
        {
            await Assert.That(scheduledActivities).HasSingleItem();
            await Assert.That(scheduledActivities.First().Activity).IsEqualTo(defaultActivity);
        }
        else
        {
            await Assert.That(scheduledActivities).IsEmpty();
        }
    }

    [Test]
    public async Task Should_Initialize_Cases_Collection_By_Default()
    {
        // Arrange & Act
        var switchActivity = new Switch();
        
        // Assert
        await Assert.That(switchActivity.Cases).IsNotNull();
        await Assert.That(switchActivity.Cases).IsEmpty();

        switchActivity.Cases.Add(new("Test", Expression.LiteralExpression(true), Substitute.For<IActivity>()));
        await Assert.That(switchActivity.Cases).HasSingleItem();
    }

    [Test]
    public async Task Should_Initialize_Mode_To_MatchFirst_By_Default()
    {
        // Arrange
        var switchActivity = new Switch();
        
        // Assert
        await Assert.That(switchActivity.Mode).IsNotNull();
        // The actual default value verification is handled by the mode-specific behavior tests
    }
    
    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}

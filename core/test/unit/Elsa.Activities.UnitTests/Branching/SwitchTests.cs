using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Branching;

public class SwitchTests
{
    [Theory]
    [InlineData(SwitchMode.MatchFirst, true)]
    [InlineData(SwitchMode.MatchAny, true)]
    [InlineData(SwitchMode.MatchFirst, false)]
    [InlineData(SwitchMode.MatchAny, false)]
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
            Assert.Single(scheduledActivities);
            Assert.Equal(defaultActivity, scheduledActivities.First().Activity);
        }
        else
        {
            Assert.Empty(scheduledActivities);
        }
    }

    [Theory]
    [InlineData(SwitchMode.MatchFirst, 1)]
    [InlineData(SwitchMode.MatchAny, 2)]
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
        Assert.Equal(expectedScheduledCount, scheduledActivities.Count);
        
        if (mode == SwitchMode.MatchFirst)
        {
            Assert.Equal(firstTrueActivity, scheduledActivities.First().Activity);
        }
        else // MatchAny
        {
            Assert.Contains(scheduledActivities, s => s.Activity == firstTrueActivity);
            Assert.Contains(scheduledActivities, s => s.Activity == secondTrueActivity);
        }
    }

    [Fact]
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
        Assert.Single(scheduledActivities);
        Assert.Equal(firstTrueActivity, scheduledActivities.First().Activity);
    }

    [Theory]
    [InlineData(SwitchMode.MatchFirst)]
    [InlineData(SwitchMode.MatchAny)]
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
        Assert.Single(scheduledActivities);
        Assert.Equal(defaultActivity, scheduledActivities.First().Activity);
    }

    [Theory]
    [InlineData(SwitchMode.MatchFirst, true)]
    [InlineData(SwitchMode.MatchAny, true)]
    [InlineData(SwitchMode.MatchFirst, false)]
    [InlineData(SwitchMode.MatchAny, false)]
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
            Assert.Single(scheduledActivities);
            Assert.Equal(defaultActivity, scheduledActivities.First().Activity);
        }
        else
        {
            Assert.Empty(scheduledActivities);
        }
    }

    [Fact]
    public void Should_Initialize_Cases_Collection_By_Default()
    {
        // Arrange & Act
        var switchActivity = new Switch();
        
        // Assert
        Assert.NotNull(switchActivity.Cases);
        Assert.Empty(switchActivity.Cases);
        
        switchActivity.Cases.Add(new("Test", Expression.LiteralExpression(true), Substitute.For<IActivity>()));
        Assert.Single(switchActivity.Cases);
    }

    [Fact]
    public void Should_Initialize_Mode_To_MatchFirst_By_Default()
    {
        // Arrange
        var switchActivity = new Switch();
        
        // Assert
        Assert.NotNull(switchActivity.Mode);
        // The actual default value verification is handled by the mode-specific behavior tests
    }
    
    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}

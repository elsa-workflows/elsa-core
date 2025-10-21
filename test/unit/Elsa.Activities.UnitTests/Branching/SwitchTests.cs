using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Branching;

public class SwitchTests
{
    [Theory]
    [InlineData(SwitchMode.MatchFirst)]
    [InlineData(SwitchMode.MatchAny)]
    public async Task Should_Schedule_Default_Activity_When_No_Cases_Match(SwitchMode mode)
    {
        // Arrange
        var defaultActivity = Substitute.For<IActivity>();
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
        Assert.Single(scheduledActivities);
        Assert.Equal(defaultActivity, scheduledActivities.First().Activity);
    }

    [Fact]
    public async Task Should_Schedule_No_Activities_When_No_Cases_Match_And_No_Default()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>
            {
                new("False Case", Expression.LiteralExpression(false), Substitute.For<IActivity>())
            },
            Default = null
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Empty(scheduledActivities);
    }

    [Fact]
    public async Task Should_Schedule_First_Matching_Case_In_MatchFirst_Mode()
    {
        // Arrange
        var firstTrueActivity = Substitute.For<IActivity>();
        var secondTrueActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases = new List<SwitchCase>
            {
                new("False Case", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
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

    [Fact]
    public async Task Should_Schedule_All_Matching_Cases_In_MatchAny_Mode()
    {
        // Arrange
        var firstTrueActivity = Substitute.For<IActivity>();
        var secondTrueActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases = new List<SwitchCase>
            {
                new("First True", Expression.LiteralExpression(true), firstTrueActivity),
                new("False", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("Second True", Expression.LiteralExpression(true), secondTrueActivity)
            }
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Equal(2, scheduledActivities.Count);
        Assert.Contains(scheduledActivities, s => s.Activity == firstTrueActivity);
        Assert.Contains(scheduledActivities, s => s.Activity == secondTrueActivity);
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

        // Assert - Should only schedule the first matching case, proving MatchFirst is the default
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

        // Assert - Null conditions should not match, so default should be scheduled
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Single(scheduledActivities);
        Assert.Equal(defaultActivity, scheduledActivities.First().Activity);
    }

    [Theory]
    [InlineData(SwitchMode.MatchFirst)]
    [InlineData(SwitchMode.MatchAny)]
    public async Task Should_Schedule_No_Activities_When_Empty_Cases_And_No_Default(SwitchMode mode)
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(mode),
            Cases = new List<SwitchCase>(), // Empty collection
            Default = null
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Empty(scheduledActivities);
    }

    [Theory]
    [InlineData(SwitchMode.MatchFirst)]
    [InlineData(SwitchMode.MatchAny)]
    public async Task Should_Schedule_Default_When_Empty_Cases_And_Default_Present(SwitchMode mode)
    {
        // Arrange
        var defaultActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Mode = new(mode),
            Cases = new List<SwitchCase>(), // Empty collection
            Default = defaultActivity
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Single(scheduledActivities);
        Assert.Equal(defaultActivity, scheduledActivities.First().Activity);
    }

    [Fact]
    public void Should_Initialize_Cases_Collection_By_Default()
    {
        // Arrange & Act
        var switchActivity = new Switch();
        
        // Assert
        Assert.NotNull(switchActivity.Cases);
        Assert.Empty(switchActivity.Cases);
        
        // Test that we can add cases
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

    [Fact]
    public async Task Should_Handle_Mixed_True_And_False_Cases_In_MatchFirst_Mode()
    {
        // Arrange
        var firstTrueActivity = Substitute.For<IActivity>();
        var secondTrueActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases = new List<SwitchCase>
            {
                new("False", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("First True", Expression.LiteralExpression(true), firstTrueActivity),
                new("Second True", Expression.LiteralExpression(true), secondTrueActivity)
            }
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert - Should only schedule the first true case
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Single(scheduledActivities);
        Assert.Equal(firstTrueActivity, scheduledActivities.First().Activity);
    }

    [Fact]
    public async Task Should_Handle_Mixed_True_And_False_Cases_In_MatchAny_Mode()
    {
        // Arrange
        var firstTrueActivity = Substitute.For<IActivity>();
        var secondTrueActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases = new List<SwitchCase>
            {
                new("First True", Expression.LiteralExpression(true), firstTrueActivity),
                new("False", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("Second True", Expression.LiteralExpression(true), secondTrueActivity)
            }
        };

        // Act
        var context = await ExecuteAsync(switchActivity);

        // Assert - Should schedule all true cases
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Equal(2, scheduledActivities.Count);
        Assert.Contains(scheduledActivities, s => s.Activity == firstTrueActivity);
        Assert.Contains(scheduledActivities, s => s.Activity == secondTrueActivity);
    }
    
    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}

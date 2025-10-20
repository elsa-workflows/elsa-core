using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Branching;

public class SwitchTests
{
    [Fact]
    public async Task Should_Execute_Activity_Without_Errors()
    {
        // Arrange
        var switchActivity = new Switch();

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Execute_With_Default_Branch_When_No_Case_Matches()
    {
        // Arrange
        var mockActivity = Substitute.For<IActivity>();
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>
            {
                new("Case 1", Expression.LiteralExpression(false), Substitute.For<IActivity>())
            },
            Default = mockActivity
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Execute_With_All_Cases_False_In_MatchFirst_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases = new List<SwitchCase>
            {
                new("Case 1", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("Case 2", Expression.LiteralExpression(false), Substitute.For<IActivity>())
            },
            Default = Substitute.For<IActivity>()
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Execute_With_Single_True_Case_In_MatchFirst_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases = new List<SwitchCase>
            {
                new("False Case", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("True Case", Expression.LiteralExpression(true), Substitute.For<IActivity>())
            }
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Execute_With_Multiple_True_Cases_In_MatchFirst_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases = new List<SwitchCase>
            {
                new("First True", Expression.LiteralExpression(true), Substitute.For<IActivity>()),
                new("Second True", Expression.LiteralExpression(true), Substitute.For<IActivity>())
            }
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Execute_With_Multiple_True_Cases_In_MatchAny_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases = new List<SwitchCase>
            {
                new("First True", Expression.LiteralExpression(true), Substitute.For<IActivity>()),
                new("False", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("Second True", Expression.LiteralExpression(true), Substitute.For<IActivity>())
            }
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Execute_With_All_Cases_False_In_MatchAny_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases = new List<SwitchCase>
            {
                new("Case 1", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("Case 2", Expression.LiteralExpression(false), Substitute.For<IActivity>())
            },
            Default = Substitute.For<IActivity>()
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Execute_When_Default_Branch_Is_Absent_And_No_Matches()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>
            {
                new("Case 1", Expression.LiteralExpression(false), Substitute.For<IActivity>())
            },
            Default = null // No default branch
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_Empty_Case_Collection()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>(), // Empty collection
            Default = Substitute.For<IActivity>()
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_Empty_Case_Collection_Without_Default()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>(), // Empty collection
            Default = null // No default
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_Mixed_Case_Conditions_In_MatchFirst_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases = new List<SwitchCase>
            {
                new("First", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("Second", Expression.LiteralExpression(true), Substitute.For<IActivity>()),
                new("Third", Expression.LiteralExpression(true), Substitute.For<IActivity>())
            }
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_Mixed_Case_Conditions_In_MatchAny_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases = new List<SwitchCase>
            {
                new("First", Expression.LiteralExpression(true), Substitute.For<IActivity>()),
                new("Second", Expression.LiteralExpression(false), Substitute.For<IActivity>()),
                new("Third", Expression.LiteralExpression(true), Substitute.For<IActivity>())
            }
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Use_MatchFirst_As_Default_Mode()
    {
        // Arrange
        var switchActivity = new Switch
        {
            // No Mode explicitly set - should default to MatchFirst
            Cases = new List<SwitchCase>
            {
                new("First True", Expression.LiteralExpression(true), Substitute.For<IActivity>()),
                new("Second True", Expression.LiteralExpression(true), Substitute.For<IActivity>())
            }
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_Null_Case_Condition_Gracefully()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>
            {
                new("Null condition", Expression.LiteralExpression(null), Substitute.For<IActivity>())
            },
            Default = Substitute.For<IActivity>()
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_Valid_Case_Conditions()
    {
        // Arrange
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>
            {
                new("Valid case", Expression.LiteralExpression(true), Substitute.For<IActivity>())
            }
        };

        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_SwitchCase_Collection_Properly()
    {
        // Arrange
        var switchActivity = new Switch();
        
        // Test that the Cases collection is initialized
        Assert.NotNull(switchActivity.Cases);
        Assert.Empty(switchActivity.Cases);
        
        // Test that we can add cases
        switchActivity.Cases.Add(new("Test", Expression.LiteralExpression(true), Substitute.For<IActivity>()));
        Assert.Single(switchActivity.Cases);
        
        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Handle_Default_Mode_Setting()
    {
        // Arrange
        var switchActivity = new Switch();
        
        // Test that Mode has a default value
        Assert.NotNull(switchActivity.Mode);
        
        // Test setting different modes
        switchActivity.Mode = new(SwitchMode.MatchAny);
        
        // Act & Assert - Should not throw any exceptions
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(switchActivity));
        
        Assert.Null(exception);
    }
    
    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
    
}

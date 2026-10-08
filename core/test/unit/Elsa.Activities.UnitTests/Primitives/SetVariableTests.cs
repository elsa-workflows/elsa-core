using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetVariableTests
{
    [Fact]
    public async Task Should_Set_Variable()
    {
        // Arrange
        const int expected = 42;
        var variable = new Variable("myVar", 0, "myVar");
        var setVariable = new SetVariable
        {
            Variable = variable,
            Value = new(expected)
        };

        // Act
        var context = await ExecuteAsync(setVariable);

        // Assert
        var result = variable.Get(context);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_Throw_When_Variable_Is_Null()
    {
        // Arrange
        var setVariable = new SetVariable
        {
            Variable = null,
            Value = new("test value")
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(setVariable));

        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task Should_Set_Variable_To_Null_Value()
    {
        // Arrange
        var variable = new Variable<string?>("myVar", "initial value", "myVar");
        var setVariable = new SetVariable
        {
            Variable = variable,
            Value = new(new Literal(null))
        };

        // Act
        var context = await ExecuteAsync(setVariable);

        // Assert
        var result = variable.Get(context);
        Assert.Null(result);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }
}
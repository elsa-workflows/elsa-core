using Elsa.Activities.UnitTests.Helpers;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetVariableTests
{
    [Fact]
    public async Task Should_Set_Variable_Integer()
    {
        // Arrange
        const int expected = 42; // The answer to life, the universe and everything.
        var variable = new Variable<int>("myVar", 0, "myVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expected));

        // Act
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.ExecuteAsync();

        // Assert
        var result = variable.Get(context);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_Throw_When_Variable_Is_Null()
    {
        // Arrange
        var setVariable = new SetVariable<string>(null!, new Input<string>("test value"));

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            var fixture = new ActivityTestFixture(setVariable);
            await fixture.ExecuteAsync();
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task Should_Set_Variable_To_Null_Value()
    {
        // Arrange
        var variable = new Variable<string?>("myVar", "initial value", "myVar");
        var setVariable = new SetVariable<string?>(variable, new Input<string?>((string?)null));

        // Act
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.ExecuteAsync();

        // Assert
        var result = variable.Get(context);
        Assert.Null(result);
    }
}
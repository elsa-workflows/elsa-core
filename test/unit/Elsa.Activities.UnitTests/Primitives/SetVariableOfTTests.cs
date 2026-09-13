using Elsa.Testing.Shared;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetVariableOfTTests
{
    [Test]
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
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Should_Throw_When_Variable_Is_Null()
    {
        // Arrange
        var setVariable = new SetVariable<string>(null!, new Input<string>("test value"));

        // Act & Assert
        await Assert.That(async () => await new ActivityTestFixture(setVariable).ExecuteAsync()).Throws<Exception>();
    }

    [Test]
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
        await Assert.That(result).IsNull();
    }
}

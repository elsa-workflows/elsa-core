using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class VariableTests
{
    [Test]
    public async Task CreateVariable_CreatesNewVariable_WithSpecifiedValue()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var variable = context.CreateVariable("testVar", 42);

        // Assert
        await Assert.That(variable).IsNotNull();
        await Assert.That(variable.Name).IsEqualTo("testVar");
        var value = context.GetVariable<int>("testVar");
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task SetVariable_CreatesOrUpdatesVariable()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.SetVariable("testVar", 10);
        context.SetVariable("testVar", 20);

        // Assert
        var value = context.GetVariable<int>("testVar");
        await Assert.That(value).IsEqualTo(20);
    }

    [Test]
    public async Task GetVariable_ReturnsNull_WhenVariableDoesNotExist()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var value = context.GetVariable<string>("nonExistent");

        // Assert
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task GetVariableValues_ReturnsAllVariables_AcrossScopes()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.SetVariable("var1", "value1");
        context.SetVariable("var2", 42);

        // Act
        var values = context.GetVariableValues();

        // Assert
        await Assert.That(values).IsNotEmpty();
        await Assert.That(values.ContainsKey("var1Variable") || values.ContainsKey("var1")).IsTrue();
        await Assert.That(values.ContainsKey("var2Variable") || values.ContainsKey("var2")).IsTrue();
    }
}
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class VariableTests
{

    [Fact]
    public async Task CreateVariable_CreatesNewVariable_WithSpecifiedValue()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var variable = context.CreateVariable("testVar", 42);

        // Assert
        Assert.NotNull(variable);
        Assert.Equal("testVar", variable.Name);
        var value = context.GetVariable<int>("testVar");
        Assert.Equal(42, value);
    }

    [Fact]
    public async Task SetVariable_CreatesOrUpdatesVariable()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.SetVariable("testVar", 10);
        context.SetVariable("testVar", 20);

        // Assert
        var value = context.GetVariable<int>("testVar");
        Assert.Equal(20, value);
    }

    [Fact]
    public async Task GetVariable_ReturnsNull_WhenVariableDoesNotExist()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var value = context.GetVariable<string>("nonExistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public async Task GetVariableValues_ReturnsAllVariables_AcrossScopes()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.SetVariable("var1", "value1");
        context.SetVariable("var2", 42);

        // Act
        var values = context.GetVariableValues();

        // Assert
        Assert.NotEmpty(values);
        Assert.True(values.ContainsKey("var1Variable") || values.ContainsKey("var1"));
        Assert.True(values.ContainsKey("var2Variable") || values.ContainsKey("var2"));
    }
}

using Elsa.Activities.UnitTests.Helpers;
using Elsa.Workflows;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetVariableTests
{
    [Fact]
    public async Task Should_Set_Variable() 
    {
        // Arrange
        var variable = new Variable<int>("myVar", 0, "myVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(42, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Should_Set_Variable_From_Expression()
    {
        // Arrange
        var variable = new Variable<string>("myStringVar", "", "myStringVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Hello World", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Hello World", result);
    }
}
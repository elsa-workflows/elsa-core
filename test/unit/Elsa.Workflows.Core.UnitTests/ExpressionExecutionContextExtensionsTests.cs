using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.UnitTests;

public class ExpressionExecutionContextExtensionsTests
{
    [Fact]
    public void GetVariable_ReturnsVariable_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new MemoryBlock(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        var result = context.GetVariable<int>("test");

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetVariable_ReturnsNull_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        var result = context.GetVariable<string>("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateVariable_ThrowsException_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new MemoryBlock(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act & Assert
        Assert.Throws<Exception>(() => context.CreateVariable("test", 10));
    }

    [Fact]
    public void CreateVariable_CreatesVariable_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.CreateVariable("newVariable", 10);

        // Assert
        var variable = context.GetVariable<int>("newVariable");
        Assert.Equal(10, variable);
    }

    [Fact]
    public void SetVariable_CreatesVariable_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.CreateVariable("newVariable", 10);

        // Assert
        var variable = context.GetVariable<int>("newVariable");
        Assert.Equal(10, variable);
    }

    [Fact]
    public void SetVariable_SetsValue_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new MemoryBlock(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.SetVariable("test", 10);

        // Assert
        var updatedVariable = context.GetVariable<int>("test");
        Assert.Equal(10, updatedVariable);
    }
}

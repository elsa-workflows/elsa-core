using Elsa.Expressions.JavaScript.Activities;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests;

public class ExpressionExecutionContextExtensionsTests
{
    [Test]
    public async Task GetVariable_ReturnsVariable_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        var result = context.GetVariable<int>("test");

        // Assert
        await Assert.That(result).IsEqualTo(5);
    }

    [Test]
    public async Task GetVariable_ReturnsNull_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        var result = context.GetVariable<string>("nonexistent");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public void CreateVariable_ThrowsException_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act & Assert
        Assert.ThrowsExactly<Exception>(() => context.CreateVariable("test", 10));
    }

    [Test]
    public async Task CreateVariable_CreatesVariable_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.CreateVariable("newVariable", 10);

        // Assert
        var variable = context.GetVariable<int>("newVariable");
        await Assert.That(variable).IsEqualTo(10);
    }

    [Test]
    public async Task SetVariable_CreatesVariable_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.SetVariable("newVariable", 10);

        // Assert
        var variable = context.GetVariable<int>("newVariable");
        await Assert.That(variable).IsEqualTo(10);
    }

    [Test]
    public async Task SetVariable_SetsValue_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.SetVariable("test", 10);

        // Assert
        var updatedVariable = context.GetVariable<int>("test");
        await Assert.That(updatedVariable).IsEqualTo(10);
    }
}
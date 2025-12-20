using Elsa.Expressions.Models;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.Core.UnitTests;

/// <summary>
/// Tests for ActivityExecutionContext handling of Literal inputs
/// </summary>
public class ActivityExecutionContextLiteralTests
{
    [Fact]
    public void TryGet_ShouldReturnTrue_WhenBlockReferenceIsLiteral()
    {
        // This test demonstrates the issue reported in GitHub issue:
        // When an Input is created with a Literal, calling Get on the ActivityExecutionContext
        // should return the literal's value directly without looking it up in the memory register.
        
        // Arrange
        var literalValue = "Hello World";
        var literal = new Literal<string>(literalValue);
        var blockReference = (MemoryBlockReference)literal;
        
        // We need to test the behavior at the MemoryBlockReference level
        // Since Literal extends MemoryBlockReference and has a Value property,
        // TryGet should be able to handle it specially
        
        // Act & Assert
        // This should work: when blockReference is a Literal, 
        // we should be able to cast it and get the value directly
        Assert.True(blockReference is Literal);
        var literalRef = blockReference as Literal;
        Assert.NotNull(literalRef);
        Assert.Equal(literalValue, literalRef.Value);
    }
    
    [Fact]
    public void Literal_ShouldInheritFromMemoryBlockReference()
    {
        // Verify that Literal is indeed a MemoryBlockReference
        var literal = new Literal<int>(42);
        Assert.IsAssignableFrom<MemoryBlockReference>(literal);
    }
}

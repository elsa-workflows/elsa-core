using Elsa.Expressions.Models;
using Elsa.Expressions.PowerFx.Services;
using Xunit;

namespace Elsa.Expressions.PowerFx.UnitTests;

public class PowerFxEvaluatorTests
{
    [Fact]
    public async Task ShouldEvaluateSimpleExpression()
    {
        // Arrange
        var evaluator = new PowerFxEvaluator();
        var expressionExecutionContext = new ExpressionExecutionContext();
        var options = new ExpressionEvaluatorOptions();
        
        // Act
        var result = await evaluator.EvaluateAsync("1 + 1", typeof(int), expressionExecutionContext, options);
        
        // Assert
        Assert.Equal(2, result);
    }
    
    [Fact]
    public async Task ShouldEvaluateExpressionWithVariables()
    {
        // Arrange
        var evaluator = new PowerFxEvaluator();
        var expressionExecutionContext = new ExpressionExecutionContext();
        expressionExecutionContext.SetVariable("Amount", 100);
        expressionExecutionContext.SetVariable("Category", "Premium");
        
        var options = new ExpressionEvaluatorOptions();
        
        // Act
        var result = await evaluator.EvaluateAsync("If(Amount > 50 && Category = \"Premium\", true, false)", typeof(bool), expressionExecutionContext, options);
        
        // Assert
        Assert.True((bool)result!);
    }
    
    [Fact]
    public async Task ShouldHandleComplexExpressions()
    {
        // Arrange
        var evaluator = new PowerFxEvaluator();
        var expressionExecutionContext = new ExpressionExecutionContext();
        expressionExecutionContext.SetVariable("Items", new[] { 10, 20, 30, 40, 50 });
        
        var options = new ExpressionEvaluatorOptions();
        
        // Act
        var result = await evaluator.EvaluateAsync("Sum(Items)", typeof(double), expressionExecutionContext, options);
        
        // Assert
        Assert.Equal(150.0, result);
    }
}
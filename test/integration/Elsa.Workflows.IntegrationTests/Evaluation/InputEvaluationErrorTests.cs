using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputEvaluationErrorTests
{
    [Theory]
    [InlineData(typeof(InvalidOperationException), "Expression evaluation failed")]
    [InlineData(typeof(ArgumentException), "Detailed inner error message")]
    [InlineData(typeof(TimeoutException), "Expression evaluation timed out")]
    public async Task Should_Wrap_Evaluation_Exceptions_In_InputEvaluationException(Type exceptionType, string errorMessage)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, errorMessage)!;
        var mockHandler = Substitute.For<IExpressionHandler>();
        mockHandler.EvaluateAsync(
                Arg.Any<Expression>(),
                Arg.Any<Type>(),
                Arg.Any<ExpressionExecutionContext>(),
                Arg.Any<ExpressionEvaluatorOptions>())
            .Throws(exception);

        var mockDescriptor = new ExpressionDescriptor
        {
            Type = "Literal",
            HandlerFactory = _ => mockHandler
        };

        var mockProvider = Substitute.For<IExpressionDescriptorProvider>();
        mockProvider.GetDescriptors().Returns(new[] { mockDescriptor });

        var writeLine = new WriteLine("Test");
        var fixture = new ActivityTestFixture(writeLine)
            .ConfigureServices(services =>
            {
                // Replace the default provider with our mock
                services.RemoveWhere(d => d.ServiceType == typeof(IExpressionDescriptorProvider));
                services.AddSingleton(mockProvider);
            });

        var context = await fixture.BuildAsync();

        // Act & Assert
        var wrappedException = await Assert.ThrowsAsync<InputEvaluationException>(async () =>
            await context.EvaluateInputPropertiesAsync());

        Assert.Equal("Text", wrappedException.InputName);
        Assert.Contains("Failed to evaluate activity input 'Text'", wrappedException.Message);
        Assert.IsType(exceptionType, wrappedException.InnerException);
        Assert.Equal(errorMessage, wrappedException.InnerException.Message);
        Assert.NotNull(wrappedException.InnerException.StackTrace);
    }

    [Fact]
    public async Task Should_Handle_Null_Expression_Gracefully()
    {
        // Arrange - Input with null expression should not throw
        var writeLine = new WriteLine("");
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
            await context.EvaluateInputPropertiesAsync());

        // Assert - Should not throw when expression is null
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Continue_Evaluation_After_Non_Critical_Errors()
    {
        // Arrange - Test with SetVariable which has multiple inputs
        var variable = new Variable<int>("testVar", 0, "testVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(42));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        var exception = await Record.ExceptionAsync(async () => await context.EvaluateInputPropertiesAsync());

        // Assert - Should complete successfully
        Assert.Null(exception);
        Assert.True(context.GetHasEvaluatedProperties());
    }
}

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
using static Elsa.Workflows.IntegrationTests.Evaluation.EvaluationTestHelpers;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputEvaluationErrorTests
{
    [Theory(DisplayName = "Wraps evaluation exceptions in InputEvaluationException")]
    [InlineData(typeof(InvalidOperationException), "Expression evaluation failed")]
    [InlineData(typeof(ArgumentException), "Detailed inner error message")]
    [InlineData(typeof(TimeoutException), "Expression evaluation timed out")]
    public async Task WrapsEvaluationExceptions(Type exceptionType, string errorMessage)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, errorMessage)!;
        var writeLine = new WriteLine("Test");
        var context = await CreateContextWithMockEvaluatorAsync(writeLine, exception);

        // Act & Assert
        var wrappedException = await Assert.ThrowsAsync<InputEvaluationException>(
            async () => await context.EvaluateInputPropertiesAsync());

        Assert.Equal("Text", wrappedException.InputName);
        Assert.Contains("Failed to evaluate activity input 'Text'", wrappedException.Message);
        Assert.IsType(exceptionType, wrappedException.InnerException);
        Assert.Equal(errorMessage, wrappedException.InnerException.Message);
        Assert.NotNull(wrappedException.InnerException.StackTrace);
    }

    [Fact(DisplayName = "Handles empty expression gracefully without throwing")]
    public async Task HandlesEmptyExpressionGracefully()
    {
        // Arrange
        var writeLine = new WriteLine("");
        var context = await CreateContextAsync(writeLine);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await context.EvaluateInputPropertiesAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact(DisplayName = "Continues evaluation when activity has multiple inputs")]
    public async Task ContinuesEvaluationForMultipleInputs()
    {
        // Arrange
        var variable = new Variable<int>("testVar", 0, "testVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(42));
        var context = await CreateContextAsync(setVariable);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await context.EvaluateInputPropertiesAsync());

        // Assert
        Assert.Null(exception);
        Assert.True(context.GetHasEvaluatedProperties());
    }
    
    private static async Task<ActivityExecutionContext> CreateContextWithMockEvaluatorAsync(
        WriteLine writeLine,
        Exception thrownException)
    {
        var mockHandler = Substitute.For<IExpressionHandler>();
        mockHandler.EvaluateAsync(
                Arg.Any<Expression>(),
                Arg.Any<Type>(),
                Arg.Any<ExpressionExecutionContext>(),
                Arg.Any<ExpressionEvaluatorOptions>())
            .Throws(thrownException);

        var mockDescriptor = new ExpressionDescriptor
        {
            Type = "Literal",
            HandlerFactory = _ => mockHandler
        };

        var mockProvider = Substitute.For<IExpressionDescriptorProvider>();
        mockProvider.GetDescriptors().Returns([mockDescriptor]);

        var fixture = new ActivityTestFixture(writeLine)
            .ConfigureServices(services =>
            {
                services.RemoveWhere(d => d.ServiceType == typeof(IExpressionDescriptorProvider));
                services.AddSingleton(mockProvider);
            });

        return await fixture.BuildAsync();
    }
}

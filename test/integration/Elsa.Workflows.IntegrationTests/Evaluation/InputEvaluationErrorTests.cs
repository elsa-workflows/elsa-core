using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputEvaluationErrorTests : EvaluationTestBase
{
    [Test]
    [DisplayName("Wraps evaluation exceptions in InputEvaluationException: $exceptionType")]
    [Arguments(typeof(InvalidOperationException), "Expression evaluation failed")]
    [Arguments(typeof(ArgumentException), "Detailed inner error message")]
    [Arguments(typeof(TimeoutException), "Expression evaluation timed out")]
    public async Task WrapsEvaluationExceptions(Type exceptionType, string errorMessage)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, errorMessage)!;
        var writeLine = new WriteLine("Test");
        var context = await CreateContextWithMockEvaluatorAsync(writeLine, exception);

        // Act & Assert
        var wrappedException = await Assert.ThrowsExactlyAsync<InputEvaluationException>(
            async () => await context.EvaluateInputPropertiesAsync());

        await Assert.That(wrappedException!.InputName).IsEqualTo("Text");
        await Assert.That(wrappedException.Message).Contains("Failed to evaluate activity input 'Text'", StringComparison.CurrentCulture);
        await Assert.That(wrappedException.InnerException).IsNotNull();
        var innerException = wrappedException.InnerException!;
        await Assert.That(innerException).IsOfType(exceptionType);
        await Assert.That(innerException.Message).IsEqualTo(errorMessage);
        await Assert.That(innerException.StackTrace).IsNotNull();
    }

    [Test]
    [DisplayName("Handles empty expression gracefully without throwing")]
    public async Task HandlesEmptyExpressionGracefully()
    {
        // Arrange
        var writeLine = new WriteLine("");
        var context = await CreateContextAsync(writeLine);

        // Act
        Exception? exception = null;
        try
        {
            await context.EvaluateInputPropertiesAsync();
        }
        catch (Exception e)
        {
            exception = e;
        }

        // Assert
        await Assert.That(exception).IsNull();
    }
    
    private async Task<ActivityExecutionContext> CreateContextWithMockEvaluatorAsync(
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

        return await OwnAsync(fixture);
    }
}

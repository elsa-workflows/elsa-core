using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Elsa.Expressions.UnitTests.Services;

public class ExpressionEvaluatorTests
{
    [Fact(DisplayName = "Evaluates expression with generic type parameter")]
    public async Task EvaluatesExpressionWithGenericTypeParameter()
    {
        // Arrange
        var context = await CreateContextAsync();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("Literal", "Test Value");

        // Act
        var result = await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext);

        // Assert
        Assert.Equal("Test Value", result);
    }

    [Fact(DisplayName = "Evaluates expression with type parameter")]
    public async Task EvaluatesExpressionWithTypeParameter()
    {
        // Arrange
        var context = await CreateContextAsync();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("Literal", 42);

        // Act
        var result = await evaluator.EvaluateAsync(expression, typeof(int), context.ExpressionExecutionContext);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact(DisplayName = "Throws when expression type not found in registry")]
    public async Task ThrowsWhenExpressionTypeNotFound()
    {
        // Arrange
        var context = await CreateContextAsync();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("NonExistentType", "value");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext));

        Assert.Contains("Could not find a descriptor for expression type", exception.Message);
        Assert.Contains("NonExistentType", exception.Message);
    }

    [Fact(DisplayName = "Resolves expression handler via descriptor factory")]
    public async Task ResolvesExpressionHandlerViaDescriptorFactory()
    {
        // Arrange
        var mockHandler = CreateMockHandler("Mocked Result");
        var context = await CreateContextWithMockHandlerAsync("CustomType", mockHandler);
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("CustomType", "test");

        // Act
        var result = await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext);

        // Assert
        Assert.Equal("Mocked Result", result);
        await mockHandler.Received(1).EvaluateAsync(
            Arg.Is<Expression>(e => e.Type == "CustomType" && e.Value as string == "test"),
            Arg.Any<Type>(),
            Arg.Any<ExpressionExecutionContext>(),
            Arg.Any<ExpressionEvaluatorOptions>());
    }

    [Fact(DisplayName = "Passes non-null options as default when not provided")]
    public async Task PassesNonNullOptionsAsDefault()
    {
        // Arrange
        var mockHandler = CreateMockHandler("Result");
        var context = await CreateContextWithMockHandlerAsync("TestType", mockHandler);
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("TestType", "value");

        // Act
        await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext);

        // Assert
        await mockHandler.Received(1).EvaluateAsync(
            Arg.Any<Expression>(),
            Arg.Any<Type>(),
            Arg.Any<ExpressionExecutionContext>(),
            Arg.Is<ExpressionEvaluatorOptions>(o => o != null));
    }

    [Fact(DisplayName = "Wraps handler exceptions in evaluation context")]
    public async Task WrapsHandlerExceptions()
    {
        // Arrange
        var mockHandler = CreateMockHandlerThatThrows(new InvalidOperationException("Handler failed"));
        var context = await CreateContextWithMockHandlerAsync("FailingType", mockHandler);
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("FailingType", "bad value");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext));

        Assert.Equal("Handler failed", exception.Message);
    }

    private static Task<ActivityExecutionContext> CreateContextAsync()
    {
        var activity = new WriteLine("Hello, World!");
        return new ActivityTestFixture(activity).BuildAsync();
    }

    private static async Task<ActivityExecutionContext> CreateContextWithMockHandlerAsync(string expressionType, IExpressionHandler mockHandler)
    {
        var mockDescriptor = new ExpressionDescriptor
        {
            Type = expressionType,
            HandlerFactory = _ => mockHandler
        };

        var mockProvider = Substitute.For<IExpressionDescriptorProvider>();
        mockProvider.GetDescriptors().Returns([mockDescriptor]);

        var activity = new WriteLine("Hello, World!");
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services =>
            {
                services.RemoveWhere(d => d.ServiceType == typeof(IExpressionDescriptorProvider));
                services.AddSingleton(mockProvider);
            });

        return await fixture.BuildAsync();
    }

    private static IExpressionHandler CreateMockHandler(object returnValue)
    {
        var mockHandler = Substitute.For<IExpressionHandler>();
        mockHandler.EvaluateAsync(
                Arg.Any<Expression>(),
                Arg.Any<Type>(),
                Arg.Any<ExpressionExecutionContext>(),
                Arg.Any<ExpressionEvaluatorOptions>())
            .Returns(returnValue);
        return mockHandler;
    }

    private static IExpressionHandler CreateMockHandlerThatThrows(Exception exception)
    {
        var mockHandler = Substitute.For<IExpressionHandler>();
        mockHandler.EvaluateAsync(
                Arg.Any<Expression>(),
                Arg.Any<Type>(),
                Arg.Any<ExpressionExecutionContext>(),
                Arg.Any<ExpressionEvaluatorOptions>())
            .Throws(exception);
        return mockHandler;
    }
}

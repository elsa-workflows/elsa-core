using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Elsa.Activities.UnitTests.Expressions;

public class ExpressionEvaluatorTests
{
    [Fact]
    public async Task Should_Evaluate_Expression_With_Generic_Type_Parameter()
    {
        // Arrange
        var activity = new WriteLine("Hello, World!");
        var context = await CreateActivityExecutionContextAsync(activity);
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("Literal", "Test Value");

        // Act
        var result = await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext);

        // Assert
        Assert.Equal("Test Value", result);
    }

    [Fact]
    public async Task Should_Evaluate_Expression_With_Type_Parameter()
    {
        // Arrange
        var activity = new WriteLine("Hello, World!");
        var context = await CreateActivityExecutionContextAsync(activity);
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("Literal", 42);

        // Act
        var result = await evaluator.EvaluateAsync(expression, typeof(int), context.ExpressionExecutionContext);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Should_Throw_When_Expression_Type_Not_Found_In_Registry()
    {
        // Arrange
        var activity = new WriteLine("Hello, World!");
        var context = await CreateActivityExecutionContextAsync(activity);
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("NonExistentType", "value");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext));

        Assert.Contains("Could not find an descriptor for expression type", exception.Message);
        Assert.Contains("NonExistentType", exception.Message);
    }

    [Fact]
    public async Task Should_Resolve_Expression_Handler_Via_Descriptor_Factory()
    {
        // Arrange
        var mockHandler = Substitute.For<IExpressionHandler>();
        mockHandler.EvaluateAsync(Arg.Any<Expression>(), Arg.Any<Type>(), Arg.Any<ExpressionExecutionContext>(), Arg.Any<ExpressionEvaluatorOptions>())
            .Returns("Mocked Result");

        var mockDescriptor = new ExpressionDescriptor
        {
            Type = "CustomType",
            HandlerFactory = _ => mockHandler
        };

        var mockProvider = Substitute.For<IExpressionDescriptorProvider>();
        mockProvider.GetDescriptors().Returns([mockDescriptor]);

        var activity = new WriteLine("Hello, World!");
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services =>
            {
                // Replace the default provider with our mock
                services.RemoveWhere(d => d.ServiceType == typeof(IExpressionDescriptorProvider));
                services.AddSingleton(mockProvider);
            });

        var context = await fixture.BuildAsync();
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

    [Fact]
    public async Task Should_Pass_Null_Options_As_Default_When_Not_Provided()
    {
        // Arrange
        var mockHandler = Substitute.For<IExpressionHandler>();
        mockHandler.EvaluateAsync(Arg.Any<Expression>(), Arg.Any<Type>(), Arg.Any<ExpressionExecutionContext>(), Arg.Any<ExpressionEvaluatorOptions>())
            .Returns("Result");

        var mockDescriptor = new ExpressionDescriptor
        {
            Type = "TestType",
            HandlerFactory = _ => mockHandler
        };

        var mockProvider = Substitute.For<IExpressionDescriptorProvider>();
        mockProvider.GetDescriptors().Returns([mockDescriptor]);

        var activity = new WriteLine("Hello, World!");
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services =>
            {
                // Replace the default provider with our mock
                services.RemoveWhere(d => d.ServiceType == typeof(IExpressionDescriptorProvider));
                services.AddSingleton(mockProvider);
            });

        var context = await fixture.BuildAsync();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("TestType", "value");

        // Act
        await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext);

        // Assert - Verify that non-null options were passed (default created by evaluator)
        await mockHandler.Received(1).EvaluateAsync(
            Arg.Any<Expression>(),
            Arg.Any<Type>(),
            Arg.Any<ExpressionExecutionContext>(),
            Arg.Is<ExpressionEvaluatorOptions>(o => o != null));
    }

    [Fact]
    public async Task Should_Wrap_Handler_Exceptions_In_Evaluation_Context()
    {
        // Arrange - QA Scenario #29: Logs errors on failed expression evaluation
        var mockHandler = Substitute.For<IExpressionHandler>();
        mockHandler.EvaluateAsync(Arg.Any<Expression>(), Arg.Any<Type>(), Arg.Any<ExpressionExecutionContext>(), Arg.Any<ExpressionEvaluatorOptions>())
            .Throws(new InvalidOperationException("Handler failed"));

        var mockDescriptor = new ExpressionDescriptor
        {
            Type = "FailingType",
            HandlerFactory = _ => mockHandler
        };

        var mockProvider = Substitute.For<IExpressionDescriptorProvider>();
        mockProvider.GetDescriptors().Returns([mockDescriptor]);

        var activity = new WriteLine("Hello, World!");
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services =>
            {
                // Replace the default provider with our mock
                services.RemoveWhere(d => d.ServiceType == typeof(IExpressionDescriptorProvider));
                services.AddSingleton(mockProvider);
            });

        var context = await fixture.BuildAsync();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expression = new Expression("FailingType", "bad value");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluator.EvaluateAsync<string>(expression, context.ExpressionExecutionContext));

        Assert.Equal("Handler failed", exception.Message);
    }
    
    private Task<ActivityExecutionContext> CreateActivityExecutionContextAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).BuildAsync();
    }
}

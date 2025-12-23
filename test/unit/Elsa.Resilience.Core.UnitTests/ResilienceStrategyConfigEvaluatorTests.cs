using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Resilience;
using Elsa.Resilience.Core.UnitTests.TestHelpers;
using Elsa.Resilience.Models;
using Elsa.Resilience.Options;
using Elsa.Resilience.Serialization;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceStrategyConfigEvaluatorTests
{
    private readonly IResilienceStrategyCatalog _catalog = Substitute.For<IResilienceStrategyCatalog>();
    private readonly IExpressionEvaluator _expressionEvaluator = Substitute.For<IExpressionEvaluator>();
    private readonly ResilienceStrategyConfigEvaluator _evaluator;
    private readonly ExpressionExecutionContext _context = new(Substitute.For<IServiceProvider>(), null!);

    public ResilienceStrategyConfigEvaluatorTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ResilienceOptions());
        var serializer = new ResilienceStrategySerializer(options);
        _evaluator = new(_catalog, _expressionEvaluator, serializer);
    }

    [Fact]
    public async Task EvaluateAsync_NullConfig_ReturnsNull()
    {
        var result = await _evaluator.EvaluateAsync(null, _context);
        Assert.Null(result);
    }

    [Fact]
    public async Task EvaluateAsync_IdentifierMode_WithValidId_ReturnsStrategyFromCatalog()
    {
        var strategy = TestDataFactory.CreateStrategy("test-strategy", "Test Strategy");
        SetupCatalogStrategy("test-strategy", strategy);
        var config = CreateConfig(ResilienceStrategyConfigMode.Identifier, "test-strategy");

        var result = await _evaluator.EvaluateAsync(config, _context);

        Assert.NotNull(result);
        Assert.Equal("test-strategy", result.Id);
        await _catalog.Received(1).GetAsync("test-strategy", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EvaluateAsync_IdentifierMode_WithInvalidId_ReturnsNull(string? strategyId)
    {
        var config = CreateConfig(ResilienceStrategyConfigMode.Identifier, strategyId);

        var result = await _evaluator.EvaluateAsync(config, _context);

        Assert.Null(result);
        await _catalog.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_ExpressionMode_WithNullExpression_ReturnsNull()
    {
        var config = CreateConfig(ResilienceStrategyConfigMode.Expression);

        var result = await _evaluator.EvaluateAsync(config, _context);

        Assert.Null(result);
        await _expressionEvaluator.DidNotReceive().EvaluateAsync<object>(Arg.Any<Expression>(), Arg.Any<ExpressionExecutionContext>(), Arg.Any<ExpressionEvaluatorOptions>());
    }

    [Fact]
    public async Task EvaluateAsync_ExpressionMode_ReturnsStringId_ResolvesFromCatalog()
    {
        var expression = new Expression("C#", "\"test-strategy\"");
        var strategy = TestDataFactory.CreateStrategy("test-strategy", "Test Strategy");
        SetupExpressionResult(expression, "test-strategy");
        SetupCatalogStrategy("test-strategy", strategy);
        var config = CreateConfig(ResilienceStrategyConfigMode.Expression, expression: expression);

        var result = await _evaluator.EvaluateAsync(config, _context);

        Assert.NotNull(result);
        Assert.Equal("test-strategy", result.Id);
        await _catalog.Received(1).GetAsync("test-strategy", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_ExpressionMode_ReturnsStrategyObject_ReturnsStrategyDirectly()
    {
        var expression = new Expression("C#", "strategy");
        var strategy = TestDataFactory.CreateStrategy("direct-strategy", "Direct Strategy");
        SetupExpressionResult(expression, strategy);
        var config = CreateConfig(ResilienceStrategyConfigMode.Expression, expression: expression);

        var result = await _evaluator.EvaluateAsync(config, _context);

        Assert.NotNull(result);
        Assert.Same(strategy, result);
        await _catalog.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("null", null)]
    [InlineData("42", 42)]
    public async Task EvaluateAsync_ExpressionMode_ReturnsUnexpectedType_ReturnsNull(string expressionCode, object? expressionResult)
    {
        var expression = new Expression("C#", expressionCode);
        SetupExpressionResult(expression, expressionResult);
        var config = CreateConfig(ResilienceStrategyConfigMode.Expression, expression: expression);

        var result = await _evaluator.EvaluateAsync(config, _context);

        Assert.Null(result);
    }
    
    private ResilienceStrategyConfig CreateConfig(ResilienceStrategyConfigMode mode, string? strategyId = null, Expression? expression = null)
    {
        return new()
        {
            Mode = mode,
            StrategyId = strategyId,
            Expression = expression
        };
    }

    private void SetupCatalogStrategy(string id, IResilienceStrategy strategy)
    {
        _catalog.GetAsync(id, Arg.Any<CancellationToken>()).Returns(strategy);
    }

    private void SetupExpressionResult(Expression expression, object? result)
    {
        _expressionEvaluator.EvaluateAsync<object>(expression, _context, Arg.Any<ExpressionEvaluatorOptions>()).Returns(result);
    }
}

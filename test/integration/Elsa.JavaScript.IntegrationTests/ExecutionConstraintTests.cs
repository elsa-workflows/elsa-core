using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Jint.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Verifies that a runaway JavaScript expression cannot occupy the calling thread indefinitely.
/// </summary>
public class ExecutionConstraintTests(ITestOutputHelper testOutputHelper)
{
    private const string InfiniteLoop = "while (true) {}";

    [Fact(DisplayName = "An expression that never returns is aborted by the execution timeout")]
    public async Task ExecutionTimeoutAbortsRunawayExpression()
    {
        var evaluator = BuildEvaluator(options => options.ExecutionTimeout = TimeSpan.FromMilliseconds(250));

        await Assert.ThrowsAsync<TimeoutException>(() => EvaluateAsync(evaluator, InfiniteLoop));
    }

    [Fact(DisplayName = "An expression that never returns is aborted when the cancellation token is signalled")]
    public async Task CancellationAbortsRunawayExpression()
    {
        var evaluator = BuildEvaluator(options => options.ExecutionTimeout = null);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        await Assert.ThrowsAsync<ExecutionCanceledException>(() => EvaluateAsync(evaluator, InfiniteLoop, cancellationTokenSource.Token));
    }

    [Fact(DisplayName = "An expression that exceeds the statement limit is aborted")]
    public async Task StatementLimitAbortsRunawayExpression()
    {
        var evaluator = BuildEvaluator(options =>
        {
            options.ExecutionTimeout = null;
            options.MaxStatements = 100;
        });

        await Assert.ThrowsAsync<StatementsCountOverflowException>(() => EvaluateAsync(evaluator, InfiniteLoop));
    }

    [Fact(DisplayName = "An expression that recurses without bound is aborted")]
    public async Task RecursionLimitAbortsRunawayExpression()
    {
        var evaluator = BuildEvaluator(options =>
        {
            options.ExecutionTimeout = null;
            options.MaxRecursionDepth = 32;
        });

        await Assert.ThrowsAsync<RecursionDepthOverflowException>(() => EvaluateAsync(evaluator, "function f() { return f(); } return f();"));
    }

    [Fact(DisplayName = "A well-behaved expression is unaffected by the default constraints")]
    public async Task DefaultConstraintsDoNotAffectNormalExpressions()
    {
        var evaluator = BuildEvaluator(_ => { });

        Assert.Equal("3", await EvaluateAsync(evaluator, "return '' + (1 + 2);"));
    }

    private IJavaScriptEvaluator BuildEvaluator(Action<JintOptions> configure)
    {
        var serviceProvider = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseJavaScript(configure))
            .Build();

        return serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    private static async Task<string?> EvaluateAsync(IJavaScriptEvaluator evaluator, string script, CancellationToken cancellationToken = default)
    {
        var context = new ExpressionExecutionContext(new ServiceCollection().BuildServiceProvider(), new());
        return (string?)await evaluator.EvaluateAsync(script, typeof(string), context, cancellationToken: cancellationToken);
    }
}

using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Jint;
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

    /// <summary>
    /// Tests whose subject is a constraint other than the timeout still register a timeout, so that a regression
    /// in the constraint under test fails that one test instead of leaving an infinite loop running until the CI
    /// job is killed. It is orders of magnitude longer than those constraints need — each of them aborts within
    /// milliseconds — so it cannot itself become a source of flaky failures on a loaded machine, and
    /// <see cref="AssertAbortedByAsync{TException}"/> reports a trip as a failsafe trip rather than as a
    /// confusing exception type mismatch.
    /// </summary>
    private static readonly TimeSpan FailsafeTimeout = TimeSpan.FromSeconds(30);

    [Fact(DisplayName = "An expression that never returns is aborted by the execution timeout")]
    public async Task ExecutionTimeoutAbortsRunawayExpression()
    {
        // No failsafe needed: the timeout is the subject here, so the test is bounded by the thing it asserts.
        var services = BuildServices(options => options.ExecutionTimeout = TimeSpan.FromMilliseconds(250));

        await Assert.ThrowsAsync<TimeoutException>(() => EvaluateAsync(services, InfiniteLoop));
    }

    [Fact(DisplayName = "An expression that never returns is aborted when the cancellation token is signalled")]
    public async Task CancellationAbortsRunawayExpression()
    {
        var services = BuildServices(options => options.ExecutionTimeout = FailsafeTimeout);
        using var cancellationTokenSource = new CancellationTokenSource();

        // The script signals the token itself, so cancellation is guaranteed to land after the engine has been
        // built and while the expression is running. A wall-clock timer would instead race engine construction
        // and could fault the setup rather than the script on a loaded machine.
        await AssertAbortedByAsync<ExecutionCanceledException>(() => EvaluateAsync(
            services,
            "cancel(); " + InfiniteLoop,
            engine => engine.SetValue("cancel", (Action)cancellationTokenSource.Cancel),
            cancellationTokenSource.Token));
    }

    [Fact(DisplayName = "An expression that exceeds the statement limit is aborted")]
    public async Task StatementLimitAbortsRunawayExpression()
    {
        var services = BuildServices(options =>
        {
            options.ExecutionTimeout = FailsafeTimeout;
            options.MaxStatements = 100;
        });

        await AssertAbortedByAsync<StatementsCountOverflowException>(() => EvaluateAsync(services, InfiniteLoop));
    }

    [Fact(DisplayName = "An expression that allocates without bound is aborted by the memory limit")]
    public async Task MemoryLimitAbortsAllocationHeavyExpression()
    {
        var services = BuildServices(options =>
        {
            options.ExecutionTimeout = FailsafeTimeout;
            options.MemoryLimit = 4 * 1024 * 1024;
        });

        // The limit is checked between statements, so the script has to allocate in visible steps. Doubling the
        // string crosses any limit within a couple of dozen iterations, which keeps the test fast and bounds how
        // far past the limit the process can get before the check fires.
        await AssertAbortedByAsync<MemoryLimitExceededException>(() => EvaluateAsync(services, "var s = 'x'; while (true) { s += s; }"));
    }

    [Fact(DisplayName = "An expression that recurses without bound is aborted")]
    public async Task RecursionLimitAbortsRunawayExpression()
    {
        var services = BuildServices(options =>
        {
            options.ExecutionTimeout = FailsafeTimeout;
            options.MaxRecursionDepth = 32;
        });

        await AssertAbortedByAsync<RecursionDepthOverflowException>(() => EvaluateAsync(services, "function f() { return f(); } return f();"));
    }

    [Fact(DisplayName = "A well-behaved expression is unaffected by the default constraints")]
    public async Task DefaultConstraintsDoNotAffectNormalExpressions()
    {
        var services = BuildServices(_ => { });

        Assert.Equal("3", await EvaluateAsync(services, "return '' + (1 + 2);"));
    }

    private IServiceProvider BuildServices(Action<JintOptions> configure)
    {
        return new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseJavaScript(configure))
            .Build();
    }

    private static async Task<string?> EvaluateAsync(IServiceProvider services, string script, Action<Engine>? configureEngine = null, CancellationToken cancellationToken = default)
    {
        var evaluator = services.GetRequiredService<IJavaScriptEvaluator>();
        var context = new ExpressionExecutionContext(services, new());

        return (string?)await evaluator.EvaluateAsync(script, typeof(string), context, configureEngine: configureEngine, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Asserts that the expression was aborted by <typeparamref name="TException"/>, distinguishing that outcome
    /// from a trip of the failsafe execution timeout that guards the test against running unbounded.
    /// </summary>
    private static async Task AssertAbortedByAsync<TException>(Func<Task> evaluate) where TException : Exception
    {
        var exception = await Record.ExceptionAsync(evaluate);

        if (exception is null)
            Assert.Fail($"Expected the expression to be aborted by {typeof(TException).Name}, but it ran to completion.");

        if (exception is TimeoutException)
            Assert.Fail($"The {FailsafeTimeout.TotalSeconds:0} second failsafe execution timeout fired before {typeof(TException).Name} was thrown: the constraint under test did not abort the expression.");

        Assert.IsType<TException>(exception);
    }
}

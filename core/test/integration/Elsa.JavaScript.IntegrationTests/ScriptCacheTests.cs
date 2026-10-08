using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Verifies the behaviour of the prepared-script cache.
/// </summary>
public class ScriptCacheTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public ScriptCacheTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Fact(DisplayName = "Repeated evaluation of the same expression produces the same result")]
    public async Task RepeatedEvaluationIsStable()
    {
        Assert.Equal("3", await EvaluateAsync("return '' + (1 + 2);"));
        Assert.Equal("3", await EvaluateAsync("return '' + (1 + 2);"));
        Assert.Equal("7", await EvaluateAsync("return '' + (3 + 4);"));
        Assert.Equal("3", await EvaluateAsync("return '' + (1 + 2);"));
    }

    [Fact(DisplayName = "An expression that fails to parse reports the failure on every evaluation")]
    public async Task ExpressionThatFailsToParseKeepsFailing()
    {
        var first = await Assert.ThrowsAnyAsync<Exception>(() => EvaluateAsync("return ("));
        var second = await Assert.ThrowsAnyAsync<Exception>(() => EvaluateAsync("return ("));

        // Every evaluation has to report the same parse failure. Merely throwing twice would also be satisfied
        // by a cache that stored a null or half-built entry for the failed preparation, because the second
        // evaluation would then fail too — just with a different exception. Comparing the two exceptions rules
        // that out. The script is identical on both evaluations, so any position the message carries is too.
        Assert.IsType(first.GetType(), second);
        Assert.Equal(first.Message, second.Message);
    }

    private async Task<string?> EvaluateAsync(string script)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context);
    }
}

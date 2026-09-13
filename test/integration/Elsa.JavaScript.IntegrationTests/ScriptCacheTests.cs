using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Verifies the behaviour of the prepared-script cache.
/// </summary>
public class ScriptCacheTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public ScriptCacheTests()
    {
        _serviceProvider = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("Repeated evaluation of the same expression produces the same result")]
    public async Task RepeatedEvaluationIsStable()
    {
        await Assert.That(await EvaluateAsync("return '' + (1 + 2);")).IsEqualTo("3");
        await Assert.That(await EvaluateAsync("return '' + (1 + 2);")).IsEqualTo("3");
        await Assert.That(await EvaluateAsync("return '' + (3 + 4);")).IsEqualTo("7");
        await Assert.That(await EvaluateAsync("return '' + (1 + 2);")).IsEqualTo("3");
    }

    [Test]
    [DisplayName("An expression that fails to parse reports the failure on every evaluation")]
    public async Task ExpressionThatFailsToParseKeepsFailing()
    {
        var first = await Assert.ThrowsAsync<Exception>(() => EvaluateAsync("return ("));
        var second = await Assert.ThrowsAsync<Exception>(() => EvaluateAsync("return ("));

        // Every evaluation has to report the same parse failure. Merely throwing twice would also be satisfied
        // by a cache that stored a null or half-built entry for the failed preparation, because the second
        // evaluation would then fail too — just with a different exception. Comparing the two exceptions rules
        // that out. The script is identical on both evaluations, so any position the message carries is too.
        await Assert.That(second).IsOfType(first!.GetType());
        await Assert.That(second!.Message).IsEqualTo(first.Message);
    }

    private async Task<string?> EvaluateAsync(string script)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context);
    }
}

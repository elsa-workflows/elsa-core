using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// The common functions — <c>getVariable</c>, <c>toJson</c>, <c>newGuid</c> and the rest — are installed as
/// lazily materialised globals, so a fresh engine builds an interop function wrapper only for the ones an
/// expression actually reads. These tests pin that the laziness is invisible: same attributes, same existence
/// and enumeration answers, and a value that is produced once and then stays put.
/// </summary>
public class CommonFunctionGlobalTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Theory(DisplayName = "A common function is a callable global")]
    [InlineData("getVariable")]
    [InlineData("getInput")]
    [InlineData("getOutputFrom")]
    [InlineData("getLastResult")]
    [InlineData("setVariable")]
    [InlineData("toJson")]
    [InlineData("newGuid")]
    [InlineData("parseGuid")]
    [InlineData("isNullOrEmpty")]
    [InlineData("streamToBytes")]
    [InlineData("getShortGuid")]
    public async Task CommonFunctionsAreCallableGlobals(string functionName)
    {
        Assert.Equal("function", await EvaluateAsync<string>($"return typeof {functionName};"));
    }

    [Fact(DisplayName = "A common function is present before anything reads its value")]
    public async Task CommonFunctionsExistWithoutBeingRead()
    {
        // The property is installed eagerly and only its value is deferred, so every existence question is
        // answered without materialising anything. `in` and getOwnPropertyNames are the questions a script — or
        // Elsa's own tooling — would ask about a function it has not called.
        Assert.Equal("true true", await EvaluateAsync<string>(
            "return ('getVariable' in globalThis) + ' ' + Object.getOwnPropertyNames(globalThis).includes('toJson');"));
    }

    [Fact(DisplayName = "A common function is not enumerable on the global object")]
    public async Task CommonFunctionsAreNotEnumerable()
    {
        // engine.SetValue(string, Delegate) installs a non-enumerable property, so these have never shown up in
        // Object.keys(globalThis). The lazy registration passes the same flag, and this is the assertion that
        // would fail if it stopped doing so. Contrast LazyTypeGlobalTests, where the type globals are enumerable
        // because engine.SetValue(string, JsValue) makes them so.
        Assert.Equal("false", await EvaluateAsync<string>("return '' + Object.keys(globalThis).includes('getVariable');"));
    }

    [Fact(DisplayName = "A common function keeps the attributes the eager registration produced")]
    public async Task CommonFunctionsKeepTheirPropertyAttributes()
    {
        Assert.Equal("true false true", await EvaluateAsync<string>(
            """
            var d = Object.getOwnPropertyDescriptor(globalThis, 'getVariable');
            return d.writable + ' ' + d.enumerable + ' ' + d.configurable;
            """));
    }

    [Fact(DisplayName = "A common function is materialised once and then stays the same value")]
    public async Task CommonFunctionsAreMaterialisedOnce()
    {
        // The factory runs at most once and the produced value is stored in the descriptor, so two reads see one
        // wrapper. A factory that re-ran per read would hand out a fresh function object each time and fail this.
        Assert.Equal("true", await EvaluateAsync<string>("return '' + (getVariable === getVariable);"));
    }

    [Fact(DisplayName = "A common function can be overwritten by a script")]
    public async Task CommonFunctionsCanBeOverwritten()
    {
        // Writable and configurable, exactly as before: a script that replaces the binding wins, whether or not
        // the original value had been materialised first.
        Assert.Equal("replaced", await EvaluateAsync<string>("toJson = function() { return 'replaced'; }; return toJson();"));
    }

    [Fact(DisplayName = "A common function that is never read still evaluates the expression")]
    public async Task UnreadCommonFunctionsDoNotAffectEvaluation()
    {
        Assert.Equal(3, await EvaluateAsync<int>("return 1 + 2;"));
    }

    private async Task<T> EvaluateAsync<T>(string script)
    {
        var context = await _fixture.CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        var result = await evaluator.EvaluateAsync(script, typeof(T), context);

        return result is T typedResult ? typedResult : (T)Convert.ChangeType(result, typeof(T))!;
    }
}

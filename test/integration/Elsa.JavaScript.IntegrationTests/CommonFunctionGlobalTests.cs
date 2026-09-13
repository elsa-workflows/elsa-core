using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// The common functions — <c>getVariable</c>, <c>toJson</c>, <c>newGuid</c> and the rest — are installed as
/// lazily materialised globals, so a fresh engine builds an interop function wrapper only for the ones an
/// expression actually reads. These tests pin that the laziness is invisible: same attributes, same existence
/// and enumeration answers, and a value that is produced once and then stays put.
/// </summary>
public class CommonFunctionGlobalTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("A common function is a callable global: $functionName")]
    [Arguments("getVariable")]
    [Arguments("getInput")]
    [Arguments("getOutputFrom")]
    [Arguments("getLastResult")]
    [Arguments("setVariable")]
    [Arguments("toJson")]
    [Arguments("newGuid")]
    [Arguments("parseGuid")]
    [Arguments("isNullOrEmpty")]
    [Arguments("streamToBytes")]
    [Arguments("getShortGuid")]
    public async Task CommonFunctionsAreCallableGlobals(string functionName)
    {
        await Assert.That(await EvaluateAsync<string>($"return typeof {functionName};")).IsEqualTo("function");
    }

    [Test]
    [DisplayName("A common function is present before anything reads its value")]
    public async Task CommonFunctionsExistWithoutBeingRead()
    {
        // The property is installed eagerly and only its value is deferred, so every existence question is
        // answered without materialising anything. `in` and getOwnPropertyNames are the questions a script — or
        // Elsa's own tooling — would ask about a function it has not called.
        await Assert.That(await EvaluateAsync<string>(
            "return ('getVariable' in globalThis) + ' ' + Object.getOwnPropertyNames(globalThis).includes('toJson');")).IsEqualTo("true true");
    }

    [Test]
    [DisplayName("A common function is not enumerable on the global object")]
    public async Task CommonFunctionsAreNotEnumerable()
    {
        // engine.SetValue(string, Delegate) installs a non-enumerable property, so these have never shown up in
        // Object.keys(globalThis). The lazy registration passes the same flag, and this is the assertion that
        // would fail if it stopped doing so. Contrast LazyTypeGlobalTests, where the type globals are enumerable
        // because engine.SetValue(string, JsValue) makes them so.
        await Assert.That(await EvaluateAsync<string>("return '' + Object.keys(globalThis).includes('getVariable');")).IsEqualTo("false");
    }

    [Test]
    [DisplayName("A common function keeps the attributes the eager registration produced")]
    public async Task CommonFunctionsKeepTheirPropertyAttributes()
    {
        await Assert.That(await EvaluateAsync<string>(
            """
            var d = Object.getOwnPropertyDescriptor(globalThis, 'getVariable');
            return d.writable + ' ' + d.enumerable + ' ' + d.configurable;
            """)).IsEqualTo("true false true");
    }

    [Test]
    [DisplayName("A common function is materialised once and then stays the same value")]
    public async Task CommonFunctionsAreMaterialisedOnce()
    {
        // The factory runs at most once and the produced value is stored in the descriptor, so two reads see one
        // wrapper. A factory that re-ran per read would hand out a fresh function object each time and fail this.
        await Assert.That(await EvaluateAsync<string>("return '' + (getVariable === getVariable);")).IsEqualTo("true");
    }

    [Test]
    [DisplayName("A common function can be overwritten by a script")]
    public async Task CommonFunctionsCanBeOverwritten()
    {
        // Writable and configurable, exactly as before: a script that replaces the binding wins, whether or not
        // the original value had been materialised first.
        await Assert.That(await EvaluateAsync<string>("toJson = function() { return 'replaced'; }; return toJson();")).IsEqualTo("replaced");
    }

    [Test]
    [DisplayName("A common function that is never read still evaluates the expression")]
    public async Task UnreadCommonFunctionsDoNotAffectEvaluation()
    {
        await Assert.That(await EvaluateAsync<int>("return 1 + 2;")).IsEqualTo(3);
    }

    private async Task<T> EvaluateAsync<T>(string script)
    {
        var context = await _fixture.CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        var result = await evaluator.EvaluateAsync(script, typeof(T), context);

        return result is T typedResult ? typedResult : (T)Convert.ChangeType(result, typeof(T))!;
    }
}

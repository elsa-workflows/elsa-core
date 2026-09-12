using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Jint;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// The generated variable, input and activity output accessors are registered only when the expression names
/// them, which by construction is invisible to an expression that names them the ordinary way. These tests pin
/// that invisibility, and pin the four escapes that turn the filtering off for expressions which reach a global
/// without naming it: globalThis, direct eval, indirect eval and the Function constructor.
/// </summary>
public class ReferencedGlobalTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "A variable accessor the expression names is registered")]
    public async Task NamedVariableAccessorsAreRegistered()
    {
        Assert.Equal(42, await EvaluateAsync<int>("return getMyVariable();"));
    }

    [Fact(DisplayName = "A variable mutator the expression names is registered")]
    public async Task NamedVariableMutatorsAreRegistered()
    {
        Assert.Equal(99, await EvaluateAsync<int>("setMyVariable(99); return getMyVariable();"));
    }

    [Fact(DisplayName = "An accessor is reachable through the dynamic functions without being named")]
    public async Task DynamicAccessorFunctionsDoNotDependOnTheGeneratedAccessors()
    {
        // getVariable is a common function rather than a generated accessor, so it is registered whatever the
        // expression references. It is what an expression that cannot name an accessor statically should use.
        Assert.Equal(42, await EvaluateAsync<int>("return getVariable('MyVariable');"));
    }

    [Fact(DisplayName = "An expression that references globalThis sees an accessor it does not name")]
    public async Task ReferencingGlobalThisRegistersEveryAccessor()
    {
        Assert.Equal("function", await EvaluateAsync<string>("return typeof globalThis['getMyVariable'];"));
    }

    [Fact(DisplayName = "An expression that calls eval directly sees an accessor it does not name")]
    public async Task CallingEvalDirectlyRegistersEveryAccessor()
    {
        Assert.Equal("function", await EvaluateAsync<string>("return eval(\"typeof getMyVariable\");"));
    }

    [Fact(DisplayName = "An expression that uses the Function constructor sees an accessor it does not name")]
    public async Task UsingTheFunctionConstructorRegistersEveryAccessor()
    {
        // Code built by the Function constructor resolves only against the global scope, so the accessor has to
        // be there. The parser does not flag this as a dynamic-code call the way it flags direct eval; it reports
        // the identifier Function instead, which is the signal to act on.
        Assert.Equal(42, await EvaluateAsync<int>("return new Function('return getMyVariable()')();"));
    }

    [Fact(DisplayName = "An expression that calls eval indirectly sees an accessor it does not name")]
    public async Task CallingEvalIndirectlyRegistersEveryAccessor()
    {
        // Indirect eval is not a direct eval call, so HasDirectEvalCall is false. As with the Function
        // constructor, the identifier eval appearing in the set is the signal.
        Assert.Equal("function", await EvaluateAsync<string>("var e = eval; return e('typeof getMyVariable');"));
    }

    [Fact(DisplayName = "An accessor the expression does not name is never registered")]
    public async Task UnnamedAccessorsAreNotRegistered()
    {
        // The tests above show the filtering is invisible from inside a script, which is also what makes it
        // unprovable from there. Holding on to the engine is the only way to see that the accessor really was
        // skipped rather than merely unused.
        var engine = await EvaluateAndCaptureEngineAsync("return 1;");

        Assert.True(engine.GetValue("getMyVariable").IsUndefined());
        Assert.True(engine.GetValue("setMyVariable").IsUndefined());
    }

    [Fact(DisplayName = "An accessor the expression names is registered on the engine")]
    public async Task NamedAccessorsAreRegisteredOnTheEngine()
    {
        var engine = await EvaluateAndCaptureEngineAsync("return getMyVariable();");

        Assert.True(engine.GetValue("getMyVariable").IsObject());
        Assert.True(engine.GetValue("setMyVariable").IsUndefined());
    }

    private async Task<T> EvaluateAsync<T>(string script)
    {
        var context = await _fixture.CreateExpressionExecutionContextAsync([new Variable<int>("MyVariable", 42)]);
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        var result = await evaluator.EvaluateAsync(script, typeof(T), context);

        return result is T typedResult ? typedResult : (T)Convert.ChangeType(result, typeof(T))!;
    }

    private async Task<Engine> EvaluateAndCaptureEngineAsync(string script)
    {
        Engine? engine = null;
        var fixture = new WorkflowTestFixture(testOutputHelper);
        fixture.ConfigureElsa(elsa => elsa.UseJavaScript(jintOptions => jintOptions.ConfigureEngine(e => engine = e)));

        var context = await fixture.CreateExpressionExecutionContextAsync([new Variable<int>("MyVariable", 42)]);
        var evaluator = fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        await evaluator.EvaluateAsync(script, typeof(object), context);

        return engine!;
    }
}

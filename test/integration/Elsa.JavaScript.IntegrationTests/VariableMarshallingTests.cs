using System.Dynamic;
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
/// Pins how an object-valued workflow variable is marshalled into JavaScript.
/// </summary>
public class VariableMarshallingTests(ITestOutputHelper testOutputHelper)
{
    [Fact(DisplayName = "An object-valued variable is readable from a script")]
    public async Task ObjectValuedVariablesAreReadable()
    {
        Assert.Equal("Alice", await EvaluateAsync<string>("return variables.Person.Name;"));
    }

    [Fact(DisplayName = "An object-valued variable is built in the shaped representation")]
    public async Task ObjectValuedVariablesAreShaped()
    {
        // Building into the shared-layout representation is a silent optimisation: JsObject.CreateFromEntries
        // falls back to the ordinary per-object property dictionary whenever it meets something a layout cannot
        // express, and the object behaves identically either way. Asking the engine is the only way to know the
        // fast one was reached. HasSharedShape is the part of that answer Jint documents as a contract — the
        // finer-grained GetObjectRepresentation names an internal representation that may be renamed or
        // subdivided in any release — and CreateFromEntries is one of its three documented success cases.
        var engine = await EvaluateAndCaptureEngineAsync("return variables.Person.Name;");
        var person = engine.Evaluate("variables.Person").AsObject();

        Assert.True(engine.Advanced.HasSharedShape(person));
    }

    private static ExpandoObject CreatePerson()
    {
        var person = new ExpandoObject() as IDictionary<string, object?>;
        person["Name"] = "Alice";
        person["Age"] = 30;
        return (ExpandoObject)person;
    }

    private async Task<T> EvaluateAsync<T>(string script)
    {
        var fixture = new WorkflowTestFixture(testOutputHelper);
        var context = await fixture.CreateExpressionExecutionContextAsync([new Variable<ExpandoObject>("Person", CreatePerson())]);
        var evaluator = fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        var result = await evaluator.EvaluateAsync(script, typeof(T), context);

        return result is T typedResult ? typedResult : (T)Convert.ChangeType(result, typeof(T))!;
    }

    private async Task<Engine> EvaluateAndCaptureEngineAsync(string script)
    {
        Engine? engine = null;
        var fixture = new WorkflowTestFixture(testOutputHelper);
        fixture.ConfigureElsa(elsa => elsa.UseJavaScript(jintOptions => jintOptions.ConfigureEngine(e => engine = e)));

        var context = await fixture.CreateExpressionExecutionContextAsync([new Variable<ExpandoObject>("Person", CreatePerson())]);
        var evaluator = fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        await evaluator.EvaluateAsync(script, typeof(object), context);

        return engine!;
    }
}

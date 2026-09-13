using System.Dynamic;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptListsAndArrays;

public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IJavaScriptEvaluator _evaluator;
    private readonly ExpressionExecutionContext _expressionContext;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        _evaluator = _services.GetRequiredService<IJavaScriptEvaluator>();
        _expressionContext = new ExpressionExecutionContext(_services, new MemoryRegister());
    }

    [Test]
    [DisplayName("Workflow inputs containing .NET lists on dynamic objects are converted to arrays for use in JavaScript.")]
    public async Task Test1()
    {
        dynamic dynamicObject = new ExpandoObject();

        dynamicObject.List = new List<string>
        {
            "a",
            "b",
            "c"
        };
        var script = "getObj().List.filter(x => x === 'b').length === 1";
        _expressionContext.SetVariable("obj", (object)dynamicObject);
        var result = await _evaluator.EvaluateAsync(script, typeof(bool), _expressionContext);

        await Assert.That((bool)result!).IsTrue();
    }

    [Test]
    [DisplayName("Can access list properties as arrays")]
    public async Task Test2()
    {
        var person = new ExpandoObject();
        var magicNumbers = new[]
        {
            42, 43, 44
        };
        var order1 = new ExpandoObject();

        order1.TryAdd("magicNumbers", magicNumbers);
        person.TryAdd("orders", new[]
        {
            order1
        });
        person.TryAdd("name", "John");
        person.TryAdd("age", 12);

        var languages = new List<object>
        {
            "English",
            "French"
        };

        person.TryAdd("languages", languages);

        var obj = new ExpandoObject();
        obj.TryAdd("persons", new List<object>
        {
            person
        });

        var name = await _evaluator.EvaluateAsync("o.persons.filter(x => x.age == 12)[0].name", typeof(string), _expressionContext, configureEngine: engine => engine.SetValue("o", obj));
        var language = await _evaluator.EvaluateAsync("o.persons[0].languages.filter(x => x == 'English')[0]", typeof(string), _expressionContext, configureEngine: engine => engine.SetValue("o", obj));
        var magicNumber = await _evaluator.EvaluateAsync("o.persons[0].orders[0].magicNumbers[1]", typeof(int), _expressionContext, configureEngine: engine => engine.SetValue("o", obj));
        await Assert.That(name).IsEqualTo("John");
        await Assert.That(language).IsEqualTo("English");
        await Assert.That(magicNumber).IsEqualTo(43);
    }

    [Test]
    [DisplayName("Can sort array and list properties as mutable arrays: $collectionType")]
    [MethodDataSource(nameof(ArraySortEnumerableData))]
    // See also:
    //  - https://github.com/sebastienros/jint/issues/1942
    //  - https://github.com/elsa-workflows/elsa-core/issues/5912
    public async Task Test5(string collectionType, IEnumerable<double> collection)
    {
        dynamic dynamicObject = new ExpandoObject();
        dynamicObject.Items = collection;
        var script = """
                     const model = variables.Model;
                     model.Items.sort((a, b) => a - b);
                     return model;
                     """;
        _expressionContext.SetVariable("Model", (object)dynamicObject);
        dynamicObject = (ExpandoObject)(await _evaluator.EvaluateAsync(script, typeof(ExpandoObject), _expressionContext))!;
        var items = ((object[])dynamicObject.Items).Cast<double>().ToArray();

        await Assert.That(items).IsEquivalentTo([2d, 4d, 8d], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public static IEnumerable<Func<(string CollectionType, IEnumerable<double> Collection)>> ArraySortEnumerableData()
    {
        yield return static () => ("array", new double[] { 8, 4, 2 });
        yield return static () => ("list", new List<double> { 8, 4, 2 });
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}

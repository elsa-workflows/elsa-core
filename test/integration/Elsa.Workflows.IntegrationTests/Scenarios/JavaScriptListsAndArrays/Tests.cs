using System.Dynamic;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptListsAndArrays;

public class Tests
{
    private readonly IJavaScriptEvaluator _evaluator;
    private readonly ExpressionExecutionContext _expressionContext;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var testOutputHelper1 = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        var services = new TestApplicationBuilder(testOutputHelper1).Build();
        _evaluator = services.GetRequiredService<IJavaScriptEvaluator>();
        _expressionContext = new ExpressionExecutionContext(services, new MemoryRegister());
    }

    [Fact(DisplayName = "Workflow inputs containing .NET lists on dynamic objects are converted to arrays for use in JavaScript.")]
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

        Assert.True((bool)result!);
    }

    [Fact(DisplayName = "Can access list properties as arrays")]
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
        Assert.Equal("John", name);
        Assert.Equal("English", language);
        Assert.Equal(43, magicNumber);
    }

    [Theory(DisplayName = "Can sort array and list properties as mutable arrays", Skip = "Not supported yet. See https://github.com/elsa-workflows/elsa-core/issues/5912")]
    [MemberData(nameof(ArraySortEnumerableData))]
    // See also:
    //  - https://github.com/sebastienros/jint/issues/1942
    //  - https://github.com/elsa-workflows/elsa-core/issues/5912
    public async Task Test5(IEnumerable<double> collection)
    {
        dynamic dynamicObject = new ExpandoObject();
        dynamicObject.Items = collection;
        var script = """
                     const model = getModel();
                     model.Items.sort((a, b) => a - b);
                     return model;
                     """;
        _expressionContext.SetVariable("Model", (object)dynamicObject);
        dynamicObject = (ExpandoObject)(await _evaluator.EvaluateAsync(script, typeof(ExpandoObject), _expressionContext))!;
        var items = ((object[])dynamicObject.Items).Cast<double>().ToArray();

        Assert.Equal([2, 4, 8], items);
    }

    public static IEnumerable<object[]> ArraySortEnumerableData = new object[][]
    {
        [
            new double[]
            {
                8, 4, 2
            }
        ],
        [
            new List<double>
            {
                8,
                4,
                2
            }
        ]
    };
}
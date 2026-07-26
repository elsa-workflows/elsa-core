using System.Dynamic;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Jint;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Verifies how CLR objects are exposed to JavaScript: array-like collections should behave like arrays,
/// while dictionary-like objects (such as the <c>variables</c> and <c>args</c> containers) should behave
/// like plain objects.
/// </summary>
public class ObjectWrappingTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public ObjectWrappingTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Fact(DisplayName = "The variables container is a plain object, not an array")]
    public async Task VariablesContainerIsNotArrayLike()
    {
        Assert.Equal("false", await EvaluateAsync<string>("return '' + (Object.getPrototypeOf(variables) === Array.prototype);"));
        Assert.Equal("undefined", await EvaluateAsync<string>("return typeof variables.map;"));
        Assert.Equal("undefined", await EvaluateAsync<string>("return typeof variables.filter;"));
        Assert.Equal("undefined", await EvaluateAsync<string>("return typeof variables.length;"));
    }

    [Fact(DisplayName = "A dictionary-like object is a plain object, not an array")]
    public async Task DictionaryLikeObjectsAreNotArrayLike()
    {
        var expando = (IDictionary<string, object?>)new ExpandoObject();
        expando["greeting"] = "hello";

        Assert.Equal("undefined", await EvaluateAsync<string>("return typeof subject.map;", engine => engine.SetValue("subject", expando)));
        Assert.Equal("hello", await EvaluateAsync<string>("return subject.greeting;", engine => engine.SetValue("subject", expando)));
        Assert.Equal("undefined", await EvaluateAsync<string>("return typeof subject.map;", engine => engine.SetValue("subject", new Dictionary<string, object> { ["greeting"] = "hello" })));
    }

    [Theory(DisplayName = "Array-like CLR collections expose the array prototype")]
    [InlineData("list")]
    [InlineData("set")]
    [InlineData("array")]
    public async Task ArrayLikeCollectionsExposeArrayPrototype(string name)
    {
        Assert.Equal("function", await EvaluateAsync<string>($"return typeof {name}.map;", ConfigureCollections));
        Assert.Equal("true", await EvaluateAsync<string>($"return '' + (Object.getPrototypeOf({name}) === Array.prototype);", ConfigureCollections));
    }

    [Theory(DisplayName = "Indexable CLR collections support array iteration methods")]
    [InlineData("list")]
    [InlineData("array")]
    public async Task IndexableCollectionsSupportArrayMethods(string name)
    {
        Assert.Equal("2,4,6", await EvaluateAsync<string>($"return {name}.map(x => x * 2).join(',');", ConfigureCollections));
        Assert.Equal("6", await EvaluateAsync<string>($"return '' + {name}.reduce((a, b) => a + b, 0);", ConfigureCollections));
    }

    private static void ConfigureCollections(Engine engine)
    {
        engine.SetValue("list", new List<int> { 1, 2, 3 });
        engine.SetValue("set", new HashSet<int> { 1, 2, 3 });
        engine.SetValue("array", new[] { 1, 2, 3 });
    }

    private async Task<T?> EvaluateAsync<T>(string script, Action<Engine>? configureEngine = null)
    {
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        return (T?)await _evaluator.EvaluateAsync(script, typeof(T), expressionExecutionContext, configureEngine: configureEngine);
    }
}

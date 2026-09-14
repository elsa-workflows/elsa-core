using System.Dynamic;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Verifies how CLR objects are exposed to JavaScript: array-like collections should behave like arrays,
/// while dictionary-like objects (such as the <c>variables</c> and <c>args</c> containers) should behave
/// like plain objects.
/// </summary>
public class ObjectWrappingTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public ObjectWrappingTests()
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
    [DisplayName("The variables container is a plain object, not an array")]
    public async Task VariablesContainerIsNotArrayLike()
    {
        await Assert.That(await EvaluateAsync<string>("return '' + (Object.getPrototypeOf(variables) === Array.prototype);")).IsEqualTo("false");
        await Assert.That(await EvaluateAsync<string>("return typeof variables.map;")).IsEqualTo("undefined");
        await Assert.That(await EvaluateAsync<string>("return typeof variables.filter;")).IsEqualTo("undefined");
        await Assert.That(await EvaluateAsync<string>("return typeof variables.length;")).IsEqualTo("undefined");
    }

    [Test]
    [DisplayName("A dictionary-like object is a plain object, not an array")]
    public async Task DictionaryLikeObjectsAreNotArrayLike()
    {
        var expando = (IDictionary<string, object?>)new ExpandoObject();
        expando["greeting"] = "hello";

        await Assert.That(await EvaluateAsync<string>("return typeof subject.map;", engine => engine.SetValue("subject", expando))).IsEqualTo("undefined");
        await Assert.That(await EvaluateAsync<string>("return subject.greeting;", engine => engine.SetValue("subject", expando))).IsEqualTo("hello");
        await Assert.That(await EvaluateAsync<string>("return typeof subject.map;", engine => engine.SetValue("subject", new Dictionary<string, object> { ["greeting"] = "hello" }))).IsEqualTo("undefined");
    }

    [Test]
    [DisplayName("Array-like CLR collections expose the array prototype")]
    [Arguments("list")]
    [Arguments("set")]
    [Arguments("array")]
    public async Task ArrayLikeCollectionsExposeArrayPrototype(string name)
    {
        await Assert.That(await EvaluateAsync<string>($"return typeof {name}.map;", ConfigureCollections)).IsEqualTo("function");
        await Assert.That(await EvaluateAsync<string>($"return '' + (Object.getPrototypeOf({name}) === Array.prototype);", ConfigureCollections)).IsEqualTo("true");
    }

    [Test]
    [DisplayName("Indexable CLR collections support array iteration methods")]
    [Arguments("list")]
    [Arguments("array")]
    public async Task IndexableCollectionsSupportArrayMethods(string name)
    {
        await Assert.That(await EvaluateAsync<string>($"return {name}.map(x => x * 2).join(',');", ConfigureCollections)).IsEqualTo("2,4,6");
        await Assert.That(await EvaluateAsync<string>($"return '' + {name}.reduce((a, b) => a + b, 0);", ConfigureCollections)).IsEqualTo("6");
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

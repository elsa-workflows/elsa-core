using System.Dynamic;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// These tests ensure that we did not replace any JS types with .NET types. 
/// </summary>
public class JavaScriptAndNetTypeTest : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public JavaScriptAndNetTypeTest()
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
    [DisplayName("Can evaluate JavaScript that returns a string")]
    public async Task ReturnsJsStringAsDotNetString()
    {
        var script = "return String('true')";
        var result = await EvaluateAsync<string>(script);
        
        await Assert.That(result).IsEqualTo("true");
    }
    
    [Test]
    [DisplayName("Can evaluate JavaScript that returns a string parsed into a Boolean")]
    public async Task ReturnsJsStringParsedAsDotNetBoolean()
    {
        var script = "return String('true')";
        var result = await EvaluateAsync<bool>(script);
        
        await Assert.That(result).IsTrue();
    }
    
    [Test]
    [DisplayName("Can evaluate JavaScript that constructs a JS Object")]
    public async Task ReturnsJsObjectAsExpandoObject()
    {
        var script = "return Object({})";
        var result = await EvaluateAsync<object>(script);
        
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsOfType(typeof(ExpandoObject));
    }

    [Test]
    [DisplayName("Can evaluate JavaScript that returns a JS Array")]
    public async Task ReturnsJsArrayAsDotNetArray()
    {
        var script = "return Array(1,2,3)";
        var result = await EvaluateAsync<int[]>(script);

        await Assert.That(result).IsNotNull();
    }

    private async Task<T?> EvaluateAsync<T>(string script)
    {
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        var type = typeof(T);
        return (T?)await _evaluator.EvaluateAsync(script, type, expressionExecutionContext);
    }
}

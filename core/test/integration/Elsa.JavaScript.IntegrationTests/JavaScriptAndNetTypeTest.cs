using System.Dynamic;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// These tests ensure that we did not replace any JS types with .NET types. 
/// </summary>
public class JavaScriptAndNetTypeTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public JavaScriptAndNetTypeTest(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Fact(DisplayName = "Can evaluate JavaScript that returns a string")]
    public async Task ReturnsJsStringAsDotNetString()
    {
        var script = "return String('true')";
        var result = await EvaluateAsync<string>(script);
        
        Assert.Equal("true", result);
    }
    
    [Fact(DisplayName = "Can evaluate JavaScript that returns a string parsed into a Boolean")]
    public async Task ReturnsJsStringParsedAsDotNetBoolean()
    {
        var script = "return String('true')";
        var result = await EvaluateAsync<bool>(script);
        
        Assert.True(result);
    }
    
    [Fact(DisplayName = "Can evaluate JavaScript that constructs a JS Object")]
    public async Task ReturnsJsObjectAsExpandoObject()
    {
        var script = "return Object({})";
        var result = await EvaluateAsync<object>(script);
        
        Assert.NotNull(result);
        Assert.IsType<ExpandoObject>(result);
    }

    [Fact(DisplayName = "Can evaluate JavaScript that returns a JS Array")]
    public async Task ReturnsJsArrayAsDotNetArray()
    {
        var script = "return Array(1,2,3)";
        var result = await EvaluateAsync<int[]>(script);

        Assert.NotNull(result);
    }

    private async Task<T?> EvaluateAsync<T>(string script)
    {
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        var type = typeof(T);
        return (T?)await _evaluator.EvaluateAsync(script, type, expressionExecutionContext);
    }
}

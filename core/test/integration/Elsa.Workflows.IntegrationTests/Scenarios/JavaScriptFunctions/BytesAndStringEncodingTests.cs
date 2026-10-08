using System.Text;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptFunctions;

public class BytesAndStringEncodingTests
{
    private readonly IJavaScriptEvaluator _evaluator;
    private readonly ExpressionExecutionContext _expressionContext;

    public BytesAndStringEncodingTests(ITestOutputHelper testOutputHelper)
    {
        var testOutputHelper1 = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        var services = new TestApplicationBuilder(testOutputHelper1).Build();
        _evaluator = services.GetRequiredService<IJavaScriptEvaluator>();
        _expressionContext = new ExpressionExecutionContext(services, new MemoryRegister());
    }

    [Fact]
    public async Task ByteArrayToString_ConvertsTo_String()
    {
        const string data = "Hello World!"; 
        var bytes = Encoding.UTF8.GetBytes(data);
        var script = "bytesToString(getData())";
        _expressionContext.SetVariable("Data", bytes);
        var result = (string)(await _evaluator.EvaluateAsync(script, typeof(string), _expressionContext))!;

        Assert.Equal(data, result);
    }
    
    [Fact]
    public async Task ByteArrayFromString_ConvertsTo_ByteArray()
    {
        const string data = "Hello World!"; 
        var script = "bytesFromString(getData())";
        _expressionContext.SetVariable("Data", data);
        var result = (byte[])(await _evaluator.EvaluateAsync(script, typeof(byte[]), _expressionContext))!;
        var bytes = Encoding.UTF8.GetBytes(data);

        Assert.Equal(bytes, result);
    }
    
    [Fact]
    public async Task ByteArrayToBase64_ConvertsTo_Base64()
    {
        const string data = "Hello World!"; 
        var bytes = Encoding.UTF8.GetBytes(data);
        var base64 = Convert.ToBase64String(bytes);
        var script = "bytesToBase64(getData())";
        _expressionContext.SetVariable("Data", bytes);
        var result = (string)(await _evaluator.EvaluateAsync(script, typeof(string), _expressionContext))!;

        Assert.Equal(base64, result);
    }
    
    [Fact]
    public async Task ByteArrayFromBase64_ConvertsTo_ByteArray()
    {
        const string data = "Hello World!"; 
        var bytes = Encoding.UTF8.GetBytes(data);
        var base64 = Convert.ToBase64String(bytes);
        var script = "bytesFromBase64(getData())";
        _expressionContext.SetVariable("Data", base64);
        var result = (byte[])(await _evaluator.EvaluateAsync(script, typeof(byte[]), _expressionContext))!;

        Assert.Equal(bytes, result);
    }
    
    [Fact]
    public async Task StringToBase64_ConvertsTo_Base64()
    {
        const string data = "Hello World!"; 
        var bytes = Encoding.UTF8.GetBytes(data);
        var base64 = Convert.ToBase64String(bytes);
        var script = "stringToBase64(getData())";
        _expressionContext.SetVariable("Data", data);
        var result = (string)(await _evaluator.EvaluateAsync(script, typeof(string), _expressionContext))!;

        Assert.Equal(base64, result);
    }
    
    [Fact]
    public async Task StringFromBase64_ConvertsTo_String()
    {
        const string data = "Hello World!"; 
        var bytes = Encoding.UTF8.GetBytes(data);
        var base64 = Convert.ToBase64String(bytes);
        var script = "stringFromBase64(getData())";
        _expressionContext.SetVariable("Data", base64);
        var result = (string)(await _evaluator.EvaluateAsync(script, typeof(string), _expressionContext))!;

        Assert.Equal(data, result);
    }
}
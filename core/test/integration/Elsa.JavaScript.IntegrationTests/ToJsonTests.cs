using System.Text.Json;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

public class ToJsonTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact(DisplayName = "Serialize large unicode string using JavaScript's toJson function")]
    public async Task Test1()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var unicodeString = UnicodeRangeGenerator.GenerateUnicodeString();
        var script = $"toJson({{ text: '{unicodeString}' }})";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new MemoryRegister());
        var result = (string)(await javaScriptEvaluator.EvaluateAsync(script, typeof(string), expressionExecutionContext))!;
        var serializedText = JsonDocument.Parse(result).RootElement.GetProperty("text").GetString();
        Assert.Equal(unicodeString, serializedText);
    }

    [Fact(DisplayName = "Serialize large unicode string using JavaScript's toJson function from a workflow variable")]
    public async Task Test2()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new MemoryRegister());
        var unicodeString = UnicodeRangeGenerator.GenerateUnicodeString();
        var payloadVariable = new Variable<object>("Payload", null!);
        var payload = new
        {
            Text = unicodeString
        };
        payloadVariable.Set(expressionExecutionContext, payload);
        var script = "toJson(getPayload())";
        var result = (string)(await javaScriptEvaluator.EvaluateAsync(script, typeof(string), expressionExecutionContext))!;
        var serializedText = JsonDocument.Parse(result).RootElement.GetProperty("Text").GetString();
        Assert.Equal(unicodeString, serializedText);
    }
}
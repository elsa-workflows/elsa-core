using System.Text.Json;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

public class ToJsonTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("Serialize large unicode string using JavaScript's toJson function")]
    public async Task Test1()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var unicodeString = UnicodeRangeGenerator.GenerateUnicodeString();
        var script = $"toJson({{ text: '{unicodeString}' }})";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new MemoryRegister());
        var result = (string)(await javaScriptEvaluator.EvaluateAsync(script, typeof(string), expressionExecutionContext))!;
        var serializedText = JsonDocument.Parse(result).RootElement.GetProperty("text").GetString();
        await Assert.That(serializedText).IsEqualTo(unicodeString);
    }

    [Test]
    [DisplayName("Serialize large unicode string using JavaScript's toJson function from a workflow variable")]
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
        await Assert.That(serializedText).IsEqualTo(unicodeString);
    }
}

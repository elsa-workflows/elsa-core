using System.Dynamic;
using System.Numerics;
using System.Text.Json;
using Elsa.Common.Converters;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;
public class JsonConverterTest : IAsyncDisposable
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
    [DisplayName("JavaScript BigInt mapping to BigInteger serialization")]
    public async Task Test1()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var script = @"return {
    'BigNumber': BigInt('7239948466988781569') 
}";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new MemoryRegister());
        var result = (await javaScriptEvaluator.EvaluateAsync(script, typeof(ExpandoObject), expressionExecutionContext))!;
        var options = new JsonSerializerOptions
        {
            Converters = { new BigIntegerJsonConverter() }
        };
        var serializedText = JsonSerializer.Serialize(result, options);

        await Assert.That(serializedText).IsEqualTo("{\"BigNumber\":7239948466988781569}");
    }

    [Test]
    [DisplayName("JavaScript BigInt mapping to BigInteger serialization")]
    public async Task Test2()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var script = @"return {
    'BigNumber': BigInt('7239948466988781569') 
}";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new MemoryRegister());
        var result = (await javaScriptEvaluator.EvaluateAsync(script, typeof(ExpandoObject), expressionExecutionContext))!;
        var serializedText = JsonSerializer.Serialize(result);

        await Assert.That(serializedText).IsEqualTo("{\"BigNumber\":{\"IsPowerOfTwo\":false,\"IsZero\":false,\"IsOne\":false,\"IsEven\":false,\"Sign\":1}}");
    }

    [Test]
    [DisplayName("BigIntegerJsonConverter Deserialize")]
    public async Task Test3()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new BigIntegerJsonConverter() }
        };

        BigInteger bigInteger = JsonSerializer.Deserialize<BigInteger>("7239948466988781569", options);

        await Assert.That(bigInteger).IsEqualTo(BigInteger.Parse("7239948466988781569"));
    }
}

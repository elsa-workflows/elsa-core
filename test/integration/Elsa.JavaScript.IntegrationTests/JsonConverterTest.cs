using System.Dynamic;
using System.Numerics;
using System.Text.Json;
using Elsa.Common.Converters;
using Elsa.Expressions.Models;
using Elsa.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;
public class JsonConverterTest(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact(DisplayName = "JavaScript BigInt mapping to BigInteger serialization")]
    public async Task Test1()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var script = @"return {
    'BigNumber': BigInt('7239948466988781569') 
}";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new MemoryRegister());
        var result = (await javaScriptEvaluator.EvaluateAsync(script, typeof(ExpandoObject), expressionExecutionContext))!;
        var options = new JsonSerializerOptions()
        {
            Converters = { new BigIntegerJsonConverter() }
        };
        var serializedText = JsonSerializer.Serialize(result, options);

        Assert.Equal("{\"BigNumber\":7239948466988781569}", serializedText);
    }

    [Fact(DisplayName = "JavaScript BigInt mapping to BigInteger serialization")]
    public async Task Test2()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var script = @"return {
    'BigNumber': BigInt('7239948466988781569') 
}";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new MemoryRegister());
        var result = (await javaScriptEvaluator.EvaluateAsync(script, typeof(ExpandoObject), expressionExecutionContext))!;
        var serializedText = JsonSerializer.Serialize(result);

        Assert.Equal("{\"BigNumber\":{\"IsPowerOfTwo\":false,\"IsZero\":false,\"IsOne\":false,\"IsEven\":false,\"Sign\":1}}", serializedText);
    }

    [Fact(DisplayName = "BigIntegerJsonConverter Deserialize")]
    public async Task Test3()
    {
        var options = new JsonSerializerOptions()
        {
            Converters = { new BigIntegerJsonConverter() }
        };

        BigInteger bigInteger = JsonSerializer.Deserialize<BigInteger>("7239948466988781569", options);

        Assert.Equal(7239948466988781569, bigInteger);
    }
}

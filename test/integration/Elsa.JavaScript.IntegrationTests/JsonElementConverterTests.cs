using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Expressions.JavaScript.IntegrationTests;

public class JsonElementConverterTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact(DisplayName = "JsonElement JsonObject can be passed to JavaScript")]
    public async Task TestJsonObjectPassedAsJsonElement()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        var jsonVariable = new Variable<object>("JsonVariable", "");
        var jsonString = "{\"name\": \"John\", \"age\": 30}";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        jsonVariable.Set(expressionExecutionContext, jsonElement);
        var script = "getVariable('JsonVariable').age";
        var result = await javaScriptEvaluator.EvaluateAsync(script, typeof(int), expressionExecutionContext);
        Assert.Equal(30, result);
    }

    [Fact(DisplayName = "JsonElement JsonArray can be passed to JavaScript")]
    public async Task TestJsonArrayPassedAsJsonElement()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        var jsonVariable = new Variable<object>("JsonVariable", "");
        var jsonString = "[1, 2, 3, 4, 5, 6]";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        jsonVariable.Set(expressionExecutionContext, jsonElement);
        var script = "getVariable('JsonVariable')[3]";
        var result = await javaScriptEvaluator.EvaluateAsync(script, typeof(int), expressionExecutionContext);
        Assert.Equal(4, result);
    }

    [Fact(DisplayName = "JsonElement string can be passed to JavaScript")]
    public async Task TestStringPassedAsJsonElement()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        var jsonVariable = new Variable<object>("JsonVariable", "");
        var jsonString = "\"I'm just a string\"";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        jsonVariable.Set(expressionExecutionContext, jsonElement);
        var script = "getVariable('JsonVariable')";
        var result = await javaScriptEvaluator.EvaluateAsync(script, typeof(string), expressionExecutionContext);
        Assert.Equal("I'm just a string", result);
    }

    [Fact(DisplayName = "JsonElement boolean can be passed to JavaScript")]
    public async Task TestBooleanPassedAsJsonElement()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        var jsonVariable = new Variable<object>("JsonVariable", "");
        var jsonString = "false";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        jsonVariable.Set(expressionExecutionContext, jsonElement);
        var script = "getVariable('JsonVariable')";
        var result = await javaScriptEvaluator.EvaluateAsync(script, typeof(bool), expressionExecutionContext);
        Assert.Equal(false, result);
    }

    [Fact(DisplayName = "JsonElement containing nested Json objects and arrays be passed to JavaScript")]
    public async Task TestNestedJsonPassedAsJsonElement()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());
        var jsonVariable = new Variable<object>("JsonVariable", "");

        var jsonString = @"
        {
            ""name"": ""John Doe"",
            ""age"": 35,
            ""address"": {
                ""street"": ""123 Main St"",
                ""city"": ""Springfield"",
                ""zip"": ""12345"",
                ""coordinates"": { ""lat"": 40.7128, ""lng"": -74.006 }
            },
            ""skills"": [
                { ""name"": ""Programming"", ""level"": ""Advanced"" },
                { ""name"": ""Writing"", ""level"": ""Intermediate"" }
            ],
            ""projects"": [
                {
                    ""title"": ""Project A"",
                    ""status"": ""Completed"",
                    ""team"": [""Alice"", ""Bob""]
                },
                {
                    ""title"": ""Project B"",
                    ""status"": ""In Progress"",
                    ""team"": [""Charlie"", ""David"", ""Eve""]
                }
            ],
            ""isEmployed"": true,
            ""contact"": { ""email"": ""john.doe@example.com"", ""phone"": ""555-1234"" }
        }";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        jsonVariable.Set(expressionExecutionContext, jsonElement);

        var script = "getVariable('JsonVariable').projects[1].team[0]";
        var result = await javaScriptEvaluator.EvaluateAsync(script, typeof(string), expressionExecutionContext);
        Assert.Equal("Charlie", result);

        var script2 = "getVariable('JsonVariable').isEmployed";
        var result2 = await javaScriptEvaluator.EvaluateAsync(script2, typeof(bool), expressionExecutionContext);
        Assert.Equal(true, result2);

        var script3 = "getVariable('JsonVariable').skills[0].level";
        var result3 = await javaScriptEvaluator.EvaluateAsync(script3, typeof(string), expressionExecutionContext);
        Assert.Equal("Advanced", result3);

        var script4 = "getVariable('JsonVariable').address.coordinates.lat";
        var result4 = await javaScriptEvaluator.EvaluateAsync(script4, typeof(double), expressionExecutionContext);
        Assert.Equal(40.7128, result4);

        var script5 = "getVariable('JsonVariable').age";
        var result5 = await javaScriptEvaluator.EvaluateAsync(script5, typeof(int), expressionExecutionContext);
        Assert.Equal(35, result5);
    }
}
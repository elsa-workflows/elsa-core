using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Jint;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Verifies which .NET types are exposed to JavaScript and under which names.
/// </summary>
public class TypeRegistrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public TypeRegistrationTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Theory(DisplayName = "Common .NET types are available under their type name")]
    [InlineData("DateTime")]
    [InlineData("DateTimeOffset")]
    [InlineData("TimeSpan")]
    [InlineData("Guid")]
    [InlineData("Random")]
    [InlineData("LogPersistenceMode")]
    [InlineData("ExpandoObject")]
    [InlineData("JsonElement")]
    [InlineData("JsonNode")]
    [InlineData("JsonObject")]
    [InlineData("Stream")]
    public async Task CommonTypesAreRegistered(string typeName)
    {
        Assert.Equal("function", await EvaluateAsync($"return typeof {typeName};"));
    }

    [Theory(DisplayName = "Types whose name is not a JavaScript identifier are not registered")]
    [InlineData("IDictionary`2")]
    [InlineData("Byte[]")]
    public async Task TypesWithUnusableNamesAreNotRegistered(string globalName)
    {
        Assert.Equal("undefined", await EvaluateAsync($"return typeof globalThis['{globalName}'];"));
    }

    [Fact(DisplayName = "A type registered by the host stays available after the built-in registrations run")]
    public async Task HostRegisteredTypesSurviveTheBuiltInRegistrations()
    {
        Assert.Equal("function", await EvaluateAsync("return typeof Uri;", engine => engine.RegisterType<Uri>()));
    }

    private async Task<string?> EvaluateAsync(string script, Action<Engine>? configureEngine = null)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context, configureEngine: configureEngine);
    }
}

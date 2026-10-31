using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Jint;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// The .NET types exposed to JavaScript are installed as lazily materialised globals. These tests pin that the
/// laziness is invisible to a script.
/// </summary>
public class LazyTypeGlobalTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public LazyTypeGlobalTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Theory(DisplayName = "Registered .NET types are available under their type name")]
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
    public async Task RegisteredTypesAreAvailable(string typeName)
    {
        Assert.Equal("function", await EvaluateAsync($"return typeof {typeName};"));
    }

    [Fact(DisplayName = "A registered type is enumerable on the global object")]
    public async Task RegisteredTypesAreEnumerable()
    {
        Assert.Equal("true", await EvaluateAsync("return '' + Object.keys(globalThis).includes('DateTime');"));
    }

    [Fact(DisplayName = "A registered type can be used")]
    public async Task RegisteredTypesCanBeUsed()
    {
        Assert.Equal("00000000-0000-0000-0000-000000000000", await EvaluateAsync("return '' + Guid.Empty;"));
    }

    [Fact(DisplayName = "A global installed by the host wins over the built-in registration of the same name")]
    public async Task HostGlobalsAreNotOverwrittenByTheBuiltInRegistrations()
    {
        // Guid is registered both as a common type and as a workflow variable type, and both registrations are
        // applied while the engine is constructed. The configureEngine callback runs afterwards, so the host's
        // value replaces the lazy global rather than the other way round.
        Assert.Equal("host-provided", await EvaluateAsync("return Guid;", engine => engine.SetValue("Guid", "host-provided")));
    }

    [Fact(DisplayName = "A type the host replaces stays replaced for the rest of the evaluation")]
    public async Task HostGlobalsSurviveRepeatedReads()
    {
        Assert.Equal("string string", await EvaluateAsync("return typeof Guid + ' ' + typeof Guid;", engine => engine.SetValue("Guid", "host-provided")));
    }

    private async Task<string?> EvaluateAsync(string script, Action<Engine>? configureEngine = null)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context, configureEngine: configureEngine);
    }
}

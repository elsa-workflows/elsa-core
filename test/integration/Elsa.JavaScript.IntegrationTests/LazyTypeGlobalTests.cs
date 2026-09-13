using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// The .NET types exposed to JavaScript are installed as lazily materialised globals. These tests pin that the
/// laziness is invisible to a script.
/// </summary>
public class LazyTypeGlobalTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public LazyTypeGlobalTests()
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
    [DisplayName("Registered .NET types are available under their type name: $typeName")]
    [Arguments("DateTime")]
    [Arguments("DateTimeOffset")]
    [Arguments("TimeSpan")]
    [Arguments("Guid")]
    [Arguments("Random")]
    [Arguments("LogPersistenceMode")]
    [Arguments("ExpandoObject")]
    [Arguments("JsonElement")]
    [Arguments("JsonNode")]
    [Arguments("JsonObject")]
    [Arguments("Stream")]
    public async Task RegisteredTypesAreAvailable(string typeName)
    {
        await Assert.That(await EvaluateAsync($"return typeof {typeName};")).IsEqualTo("function");
    }

    [Test]
    [DisplayName("A registered type is enumerable on the global object")]
    public async Task RegisteredTypesAreEnumerable()
    {
        await Assert.That(await EvaluateAsync("return '' + Object.keys(globalThis).includes('DateTime');")).IsEqualTo("true");
    }

    [Test]
    [DisplayName("A registered type can be used")]
    public async Task RegisteredTypesCanBeUsed()
    {
        await Assert.That(await EvaluateAsync("return '' + Guid.Empty;")).IsEqualTo("00000000-0000-0000-0000-000000000000");
    }

    [Test]
    [DisplayName("A global installed by the host wins over the built-in registration of the same name")]
    public async Task HostGlobalsAreNotOverwrittenByTheBuiltInRegistrations()
    {
        // Guid is registered both as a common type and as a workflow variable type, and both registrations are
        // applied while the engine is constructed. The configureEngine callback runs afterwards, so the host's
        // value replaces the lazy global rather than the other way round.
        await Assert.That(await EvaluateAsync("return Guid;", engine => engine.SetValue("Guid", "host-provided"))).IsEqualTo("host-provided");
    }

    [Test]
    [DisplayName("A type the host replaces stays replaced for the rest of the evaluation")]
    public async Task HostGlobalsSurviveRepeatedReads()
    {
        await Assert.That(await EvaluateAsync("return typeof Guid + ' ' + typeof Guid;", engine => engine.SetValue("Guid", "host-provided"))).IsEqualTo("string string");
    }

    private async Task<string?> EvaluateAsync(string script, Action<Engine>? configureEngine = null)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context, configureEngine: configureEngine);
    }
}

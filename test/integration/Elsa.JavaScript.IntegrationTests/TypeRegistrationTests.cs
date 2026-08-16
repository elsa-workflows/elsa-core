using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;
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

    [Fact(DisplayName = "A global installed by the host wins over the built-in registration of the same name")]
    public async Task HostGlobalsAreNotOverwrittenByTheBuiltInRegistrations()
    {
        // Guid is registered lazily while the engine options are being built. The configureEngine callback runs
        // afterwards, so a host can deliberately replace that built-in global.
        Assert.Equal("host-provided", await EvaluateAsync("return Guid;", engine => engine.SetValue("Guid", "host-provided")));
    }

    [Fact(DisplayName = "A registered type is exposed as a CLR type reference and registering it again is a no-op")]
    public async Task RegisterTypeInstallsATypeReferenceAndIsIdempotent()
    {
        JsValue afterFirstRegistration = JsValue.Undefined;
        JsValue afterSecondRegistration = JsValue.Undefined;

        await EvaluateAsync("return 'done';", engine =>
        {
            engine.RegisterType<Uri>();
            afterFirstRegistration = engine.GetValue("Uri");
            engine.RegisterType<Uri>();
            afterSecondRegistration = engine.GetValue("Uri");
        });

        var typeReference = Assert.IsType<TypeReference>(afterFirstRegistration);
        Assert.Equal(typeof(Uri), typeReference.ReferenceType);
        Assert.Same(afterFirstRegistration, afterSecondRegistration);
    }

    private async Task<string?> EvaluateAsync(string script, Action<Engine>? configureEngine = null)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context, configureEngine: configureEngine);
    }
}

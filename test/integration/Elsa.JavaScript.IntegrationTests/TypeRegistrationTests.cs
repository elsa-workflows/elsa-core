using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Verifies which .NET types are exposed to JavaScript and under which names.
/// </summary>
public class TypeRegistrationTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public TypeRegistrationTests()
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
    [DisplayName("Common .NET types are available under their type name: $typeName")]
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
    public async Task CommonTypesAreRegistered(string typeName)
    {
        await Assert.That(await EvaluateAsync($"return typeof {typeName};")).IsEqualTo("function");
    }

    [Test]
    [DisplayName("Types whose name is not a JavaScript identifier are not registered: $globalName")]
    [Arguments("IDictionary`2")]
    [Arguments("Byte[]")]
    public async Task TypesWithUnusableNamesAreNotRegistered(string globalName)
    {
        await Assert.That(await EvaluateAsync($"return typeof globalThis['{globalName}'];")).IsEqualTo("undefined");
    }

    [Test]
    [DisplayName("A type registered by the host stays available after the built-in registrations run")]
    public async Task HostRegisteredTypesSurviveTheBuiltInRegistrations()
    {
        await Assert.That(await EvaluateAsync("return typeof Uri;", engine => engine.RegisterType<Uri>())).IsEqualTo("function");
    }

    [Test]
    [DisplayName("A global installed by the host wins over the built-in registration of the same name")]
    public async Task HostGlobalsAreNotOverwrittenByTheBuiltInRegistrations()
    {
        // Guid is registered lazily while the engine options are being built. The configureEngine callback runs
        // afterwards, so a host can deliberately replace that built-in global.
        await Assert.That(await EvaluateAsync("return Guid;", engine => engine.SetValue("Guid", "host-provided"))).IsEqualTo("host-provided");
    }

    [Test]
    [DisplayName("A registered type is exposed as a CLR type reference and registering it again is a no-op")]
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

        await Assert.That(afterFirstRegistration).IsOfType(typeof(TypeReference));
        var typeReference = (TypeReference)afterFirstRegistration;
        await Assert.That(typeReference.ReferenceType).IsEqualTo(typeof(Uri));
        await Assert.That(afterSecondRegistration).IsSameReferenceAs(afterFirstRegistration);
    }

    private async Task<string?> EvaluateAsync(string script, Action<Engine>? configureEngine = null)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context, configureEngine: configureEngine);
    }
}

using System.Text.Json;
using Elsa.Testing.Shared;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConverterInvokerTests
{
    private const string ConverterId = "tests.output-converter";

    [Fact]
    public async Task Invoke_WhenDeclaredSourceDerivesFromSupportedSource_InvokesConverter()
    {
        var converter = new RecordingConverter(_ => "converted");
        var context = await CreateContextAsync(ConverterId, converter);
        var invoker = CreateInvoker(new(ConverterId, typeof(Animal), typeof(string), "Test"));

        var result = invoker.Invoke(
            context,
            CreateOutput(),
            "Result",
            typeof(Dog),
            new Dog(),
            Destination(typeof(string)));

        Assert.Equal("converted", result);
        Assert.Equal(1, converter.ConvertCalls);
    }

    [Fact]
    public async Task Invoke_WhenDeclaredSourceIsIncompatible_FailsBeforeConversion()
    {
        var converter = new RecordingConverter(_ => "converted");
        var context = await CreateContextAsync(ConverterId, converter);
        var invoker = CreateInvoker(new(ConverterId, typeof(Stream), typeof(string), "Test"));

        var exception = Assert.Throws<OutputConversionException>(() => invoker.Invoke(
            context,
            CreateOutput(),
            "Result",
            typeof(Dog),
            new Dog(),
            Destination(typeof(string))));

        Assert.Equal(OutputConversionFailureStage.SourceCompatibility, exception.Stage);
        Assert.Equal(0, converter.ConvertCalls);
    }

    [Fact]
    public async Task Invoke_WhenSettingsViolateJsonSchema_FailsBeforeConversion()
    {
        var converter = new RecordingConverter(_ => "converted");
        var context = await CreateContextAsync(ConverterId, converter);
        var schema = ParseJson(
            """
            {
              "type": "object",
              "required": ["prefix"],
              "properties": {
                "prefix": { "type": "string" }
              }
            }
            """);
        var invoker = CreateInvoker(new(ConverterId, typeof(int), typeof(string), "Test", settingsSchema: schema));
        var output = CreateOutput(ParseJson("{}"));

        var exception = Assert.Throws<OutputConversionException>(() => invoker.Invoke(
            context,
            output,
            "Result",
            typeof(int),
            42,
            Destination(typeof(string))));

        Assert.Equal(OutputConversionFailureStage.SettingsValidation, exception.Stage);
        Assert.Equal(0, converter.ConvertCalls);
    }

    [Fact]
    public async Task Invoke_WhenConverterRejectsSettings_FailsBeforeConversion()
    {
        var converter = new RecordingConverter(
            _ => "converted",
            _ => ["The configured format is unsupported."]);
        var context = await CreateContextAsync(ConverterId, converter);
        var invoker = CreateInvoker(new(ConverterId, typeof(int), typeof(string), "Test"));

        var exception = Assert.Throws<OutputConversionException>(() => invoker.Invoke(
            context,
            CreateOutput(ParseJson("""{ "format": "unsupported" }""")),
            "Result",
            typeof(int),
            42,
            Destination(typeof(string))));

        Assert.Equal(OutputConversionFailureStage.SettingsValidation, exception.Stage);
        Assert.Equal(0, converter.ConvertCalls);
    }

    [Fact]
    public async Task Invoke_WhenDeclaredResultCannotBeAssignedToDestination_FailsBeforeConversion()
    {
        var converter = new RecordingConverter(_ => 42);
        var context = await CreateContextAsync(ConverterId, converter);
        var invoker = CreateInvoker(new(ConverterId, typeof(int), typeof(int), "Test"));

        var exception = Assert.Throws<OutputConversionException>(() => invoker.Invoke(
            context,
            CreateOutput(),
            "Result",
            typeof(int),
            1,
            Destination(typeof(string))));

        Assert.Equal(OutputConversionFailureStage.ResultValidation, exception.Stage);
        Assert.Equal(0, converter.ConvertCalls);
    }

    [Fact]
    public async Task Invoke_WhenRuntimeResultViolatesDescriptor_FailsResultValidation()
    {
        var converter = new RecordingConverter(_ => 42);
        var context = await CreateContextAsync(ConverterId, converter);
        var invoker = CreateInvoker(new(ConverterId, typeof(int), typeof(string), "Test"));

        var exception = Assert.Throws<OutputConversionException>(() => invoker.Invoke(
            context,
            CreateOutput(),
            "Result",
            typeof(int),
            1,
            Destination(typeof(object))));

        Assert.Equal(OutputConversionFailureStage.ResultValidation, exception.Stage);
        Assert.Equal(1, converter.ConvertCalls);
    }

    [Fact]
    public async Task Invoke_WhenConverterReturnsNullForNonNullableDestination_FailsResultValidation()
    {
        var converter = new RecordingConverter(_ => null);
        var context = await CreateContextAsync(ConverterId, converter);
        var invoker = CreateInvoker(new(ConverterId, typeof(int), typeof(int?), "Test"));

        var exception = Assert.Throws<OutputConversionException>(() => invoker.Invoke(
            context,
            CreateOutput(),
            "Result",
            typeof(int),
            1,
            Destination(typeof(int), allowsNull: false)));

        Assert.Equal(OutputConversionFailureStage.ResultValidation, exception.Stage);
    }

    [Fact]
    public async Task Invoke_ResolvesScopedKeyedConverterFromEachWorkflowProvider()
    {
        var descriptor = new OutputConverterDescriptor(ConverterId, typeof(int), typeof(Guid), "Test");
        var invoker = CreateInvoker(descriptor);
        var firstContext = await CreateScopedContextAsync();
        var secondContext = await CreateScopedContextAsync();
        var output = CreateOutput();
        var destination = Destination(typeof(Guid));

        var firstResult = invoker.Invoke(firstContext, output, "Result", typeof(int), 1, destination);
        var repeatedFirstResult = invoker.Invoke(firstContext, output, "Result", typeof(int), 2, destination);
        var secondResult = invoker.Invoke(secondContext, output, "Result", typeof(int), 3, destination);

        Assert.Equal(firstResult, repeatedFirstResult);
        Assert.NotEqual(firstResult, secondResult);
    }

    private static OutputConverterInvoker CreateInvoker(OutputConverterDescriptor descriptor)
    {
        var registration = new OutputConverterRegistration(descriptor, descriptor.Id, ServiceLifetime.Scoped);
        var registry = new OutputConverterRegistry([registration]);
        return new(registry, new OutputConverterSettingsValidator());
    }

    private static Output<int> CreateOutput(JsonElement? settings = null) =>
        new()
        {
            Converter = new(ConverterId, settings)
        };

    private static OutputBindingDestination Destination(Type type, bool? allowsNull = null) =>
        new(
            "destination",
            type,
            allowsNull ?? (!type.IsValueType || Nullable.GetUnderlyingType(type) != null),
            OutputBindingDestinationKind.Variable);

    private static async Task<ActivityExecutionContext> CreateContextAsync(string id, IOutputConverter converter)
    {
        var fixture = new ActivityTestFixture(new TestActivity())
            .ConfigureServices(services => services.AddKeyedSingleton(id, converter));
        return await fixture.BuildAsync();
    }

    private static async Task<ActivityExecutionContext> CreateScopedContextAsync()
    {
        var fixture = new ActivityTestFixture(new TestActivity())
            .ConfigureServices(services =>
            {
                services.AddScoped<ScopedToken>();
                services.AddKeyedScoped<IOutputConverter, ScopeAwareConverter>(ConverterId);
            });
        return await fixture.BuildAsync();
    }

    private static JsonElement ParseJson(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private sealed class TestActivity : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }

    private class Animal
    {
    }

    private sealed class Dog : Animal
    {
    }

    private sealed class RecordingConverter(
        Func<OutputConversionContext, object?> convert,
        Func<JsonElement?, IEnumerable<string>>? validate = null) : IOutputConverter
    {
        public int ConvertCalls { get; private set; }

        public object? Convert(OutputConversionContext context)
        {
            ConvertCalls++;
            return convert(context);
        }

        public IEnumerable<string> ValidateSettings(JsonElement? settings) => validate?.Invoke(settings) ?? [];
    }

    private sealed class ScopedToken
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class ScopeAwareConverter(ScopedToken token) : IOutputConverter
    {
        public object Convert(OutputConversionContext context) => token.Id;
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Common.Serialization;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Options;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public sealed class SerializationTypeResolverTests
{
    private static readonly string UnsafeAssemblyQualifiedTypeAlias = typeof(System.Text.StringBuilder).AssemblyQualifiedName!;
    private readonly SerializationTypeRegistry _workflowJsonTypeRegistry = new(Microsoft.Extensions.Options.Options.Create(new SerializationTypeOptions()));
    private readonly JsonSerializerOptions _options;

    public SerializationTypeResolverTests()
    {
        _workflowJsonTypeRegistry.RegisterType(typeof(ExceptionState), nameof(ExceptionState));
        _workflowJsonTypeRegistry.RegisterType(typeof(FaultException), nameof(FaultException));
        _workflowJsonTypeRegistry.RegisterType(typeof(RegisteredPayload), "RegisteredPayload");
        _options = CreateOptions(_workflowJsonTypeRegistry);
    }

    [Test]
    [Arguments("String", typeof(string))]
    [Arguments("String[]", typeof(string[]))]
    [Arguments("String[][]", typeof(string[][]))]
    [Arguments("List<String>", typeof(List<string>))]
    [Arguments("List<String[]>", typeof(List<string[]>))]
    [Arguments("List<List<String>>", typeof(List<List<string>>))]
    [Arguments("ExceptionState", typeof(ExceptionState))]
    [Arguments("FaultException", typeof(FaultException))]
    [Arguments("ObjectDictionary", typeof(IDictionary<string, object>))]
    public async Task When_DeserializeRegisteredTypeAlias_Then_ReturnsExpectedType(string typeAlias, Type expectedType)
    {
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _options);

        await Assert.That(result).IsEqualTo(expectedType);
    }

    [Test]
    public async Task When_DeserializeRegisteredLegacyAssemblyQualifiedTypeAlias_Then_ReturnsExpectedType()
    {
        var typeAlias = typeof(RegisteredPayload).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonString(typeAlias), _options);

        await Assert.That(result).IsEqualTo(typeof(RegisteredPayload));
    }

    [Test]
    public async Task When_DeserializeRegisteredTypeAliasWithDifferentCasing_Then_ReturnsExpectedType()
    {
        var result = JsonSerializer.Deserialize<Type>(JsonString("registeredpayload"), _options);

        await Assert.That(result).IsEqualTo(typeof(RegisteredPayload));
    }

    [Test]
    public async Task When_DeserializeRegisteredLegacyGenericCollectionTypeAlias_Then_ReturnsExpectedType()
    {
        var typeAlias = typeof(List<RegisteredPayload>).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonString(typeAlias), _options);

        await Assert.That(result).IsEqualTo(typeof(List<RegisteredPayload>));
    }

    [Test]
    [Arguments(typeof(string), "String")]
    [Arguments(typeof(string[]), "String[]")]
    [Arguments(typeof(string[][]), "String[][]")]
    [Arguments(typeof(List<string>), "List<String>")]
    [Arguments(typeof(List<string[]>), "List<String[]>")]
    [Arguments(typeof(List<List<string>>), "List<List<String>>")]
    [Arguments(typeof(ExceptionState), "ExceptionState")]
    [Arguments(typeof(FaultException), "FaultException")]
    public async Task When_SerializeSupportedType_Then_EmitsAliasThatCanBeDeserialized(Type type, string expectedAlias)
    {
        var json = JsonSerializer.Serialize(type, _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        await Assert.That(alias).IsEqualTo(expectedAlias);
        await Assert.That(result).IsEqualTo(type);
    }

    [Test]
    [Arguments(typeof(IEnumerable<string>), "List<String>", typeof(List<string>))]
    [Arguments(typeof(ICollection<string>), "List<String>", typeof(List<string>))]
    [Arguments(typeof(IList<string>), "List<String>", typeof(List<string>))]
    [Arguments(typeof(IReadOnlyCollection<string>), "List<String>", typeof(List<string>))]
    [Arguments(typeof(IReadOnlyList<string>), "List<String>", typeof(List<string>))]
    [Arguments(typeof(ISet<string>), "HashSet<String>", typeof(HashSet<string>))]
    public async Task When_SerializeInterfaceCollectionType_Then_EmitsInstantiableAlias(Type type, string expectedAlias, Type expectedRoundTripType)
    {
        var json = JsonSerializer.Serialize(type, _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        await Assert.That(alias).IsEqualTo(expectedAlias);
        await Assert.That(result).IsEqualTo(expectedRoundTripType);
    }

    [Test]
    [MethodDataSource(nameof(JsonIslandValues))]
    public async Task When_SerializeSpecialJsonIslandType_Then_CanBeDeserialized(object value, Type expectedType)
    {
        _workflowJsonTypeRegistry.RegisterType(typeof(JObject), nameof(JObject));
        _workflowJsonTypeRegistry.RegisterType(typeof(JArray), nameof(JArray));

        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<object>(json, _options);

        await Assert.That(result).IsOfType(expectedType);
    }

    [Test]
    [Arguments(typeof(System.Text.StringBuilder))]
    [Arguments(typeof(System.Text.StringBuilder[]))]
    [Arguments(typeof(List<System.Text.StringBuilder>))]
    public async Task When_SerializeUnsupportedType_Then_EmitsSafeUnregisteredTypeAlias(Type type)
    {
        var json = JsonSerializer.Serialize(type, _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        await Assert.That(alias).StartsWith("UnregisteredClrType:");
        await Assert.That(result).IsEqualTo(typeof(Exception));
    }

    [Test]
    public async Task When_SerializeExceptionStateWithUnregisteredExceptionType_Then_DoesNotThrow()
    {
        var exceptionState = ExceptionState.FromException(new NullReferenceException("Test"));

        var json = JsonSerializer.Serialize(exceptionState, _options);
        var result = JsonSerializer.Deserialize<ExceptionState>(json, _options)!;

        await Assert.That(json).Contains("UnregisteredClrType:");
        await Assert.That(result.Type).IsEqualTo(typeof(Exception));
        await Assert.That(result.Message).IsEqualTo("Test");
    }

    [Test]
    public async Task When_ConfigureWorkflowsFeature_Then_RegistersCoreAliases()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.UseWorkflows();
        module.Apply();
        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<ISerializationTypeRegistry>();

        var aliasRegistered = registry.TryGetAlias(typeof(NullReferenceException), out var alias);
        var typeRegistered = registry.TryGetType(nameof(NullReferenceException), out var type);

        await Assert.That(aliasRegistered).IsTrue();
        await Assert.That(alias).IsEqualTo(nameof(NullReferenceException));
        await Assert.That(typeRegistered).IsTrue();
        await Assert.That(type).IsEqualTo(typeof(NullReferenceException));
        await Assert.That(registry.TryGetAlias(typeof(MemoryStorageDriver), out var memoryStorageDriverAlias)).IsTrue();
        await Assert.That(memoryStorageDriverAlias).IsEqualTo(nameof(MemoryStorageDriver));
        await Assert.That(registry.TryGetType(nameof(MemoryStorageDriver), out var memoryStorageDriverType)).IsTrue();
        await Assert.That(memoryStorageDriverType).IsEqualTo(typeof(MemoryStorageDriver));
        await Assert.That(registry.TryGetType(typeof(MemoryStorageDriver).GetSimpleAssemblyQualifiedName(), out var legacyMemoryStorageDriverType)).IsTrue();
        await Assert.That(legacyMemoryStorageDriverType).IsEqualTo(typeof(MemoryStorageDriver));
        await Assert.That(registry.TryGetAlias(typeof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy), out var incidentStrategyAlias)).IsTrue();
        await Assert.That(incidentStrategyAlias).IsEqualTo(nameof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy));
        await Assert.That(registry.TryGetType(typeof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy).GetSimpleAssemblyQualifiedName(), out var legacyIncidentStrategyType)).IsTrue();
        await Assert.That(legacyIncidentStrategyType).IsEqualTo(typeof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy));
    }

    [Test]
    public async Task When_TypeAliasExistsOnlyInExpressionOptions_Then_WorkflowJsonDoesNotResolveIt()
    {
        var expressionOptions = new ExpressionOptions();
        expressionOptions.RegisterTypeAlias(typeof(ExpressionOnlyPayload), "ExpressionOnlyPayload");
        var expressionRegistry = new WellKnownTypeRegistry(Microsoft.Extensions.Options.Options.Create(expressionOptions));

        await Assert.That(expressionRegistry.TryGetType("ExpressionOnlyPayload", out _)).IsTrue();
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonString("ExpressionOnlyPayload"), _options));
    }

    [Test]
    public async Task When_SerializePolymorphicObjectWithUnregisteredType_Then_OmitsTypeMetadata()
    {
        var json = JsonSerializer.Serialize<object>(new UnregisteredPayload { Name = "Alice" }, _options);

        var result = JsonSerializer.Deserialize<object>(json, _options);

        await Assert.That(json).DoesNotContain("\"_type\"");
        var payload = (await Assert.That(result).IsAssignableTo<IDictionary<string, object>>())!;
        await Assert.That(payload["name"]).IsEqualTo("Alice");
    }

    [Test]
    public void When_DeserializeUnknownAssemblyQualifiedTypeAlias_Then_ThrowsJsonException()
    {
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonString(UnsafeAssemblyQualifiedTypeAlias), _options));
    }

    [Test]
    public void When_DeserializeUnknownGenericElementTypeAlias_Then_ThrowsJsonException()
    {
        var typeAlias = $"List<{UnsafeAssemblyQualifiedTypeAlias}>";

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonString(typeAlias), _options));
    }

    [Test]
    public async Task When_DeserializePolymorphicObjectWithRegisteredTypeAlias_Then_ReturnsTypedObject()
    {
        var json = """
        {
          "name": "Alice",
          "_type": "RegisteredPayload"
        }
        """;

        var result = JsonSerializer.Deserialize<object>(json, _options);

        var payload = (await Assert.That(result).IsTypeOf<RegisteredPayload>())!;
        await Assert.That(payload.Name).IsEqualTo("Alice");
    }

    [Test]
    [Arguments("IEnumerable<String>", typeof(List<string>))]
    [Arguments("ICollection<String>", typeof(List<string>))]
    [Arguments("IList<String>", typeof(List<string>))]
    [Arguments("IReadOnlyCollection<String>", typeof(List<string>))]
    [Arguments("IReadOnlyList<String>", typeof(List<string>))]
    [Arguments("ISet<String>", typeof(HashSet<string>))]
    public async Task When_DeserializePolymorphicCollectionInterface_Then_ReturnsConcreteCollection(string typeAlias, Type expectedType)
    {
        var json = $$"""
        {
          "_items": ["Alice"],
          "_type": "{{typeAlias}}"
        }
        """;

        var result = JsonSerializer.Deserialize<object>(json, _options);

        await Assert.That(result).IsOfType(expectedType);
    }

    [Test]
    public void When_DeserializePolymorphicObjectWithNonInstantiableType_Then_ThrowsJsonException()
    {
        _workflowJsonTypeRegistry.RegisterType(typeof(AbstractPayload), "AbstractPayload");
        var json = """
        {
          "_type": "AbstractPayload"
        }
        """;

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<object>(json, _options));
    }

    [Test]
    public void When_DeserializePolymorphicObjectWithUnknownAssemblyQualifiedType_Then_ThrowsJsonException()
    {
        var json = $$"""
        {
          "capacity": 16,
          "_type": {{JsonString(UnsafeAssemblyQualifiedTypeAlias)}}
        }
        """;

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<object>(json, _options));
    }

    [Test]
    public void When_DeserializePolymorphicObjectWithoutTypeJsonConverterAndUnknownAssemblyQualifiedType_Then_ThrowsJsonException()
    {
        var options = CreatePolymorphicOnlyOptions(_workflowJsonTypeRegistry);
        var json = $$"""
        {
          "capacity": 16,
          "_type": {{JsonString(UnsafeAssemblyQualifiedTypeAlias)}}
        }
        """;

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<object>(json, options));
    }

    [Test]
    public async Task When_DeserializeDictionaryObjectPayloadWithRegisteredTypeAlias_Then_ReturnsTypedObjectValue()
    {
        var json = """
        {
          "payload": {
            "name": "Alice",
            "_type": "RegisteredPayload"
          }
        }
        """;

        var result = JsonSerializer.Deserialize<IDictionary<string, object>>(json, _options)!;

        var payload = (await Assert.That(result["payload"]).IsTypeOf<RegisteredPayload>())!;
        await Assert.That(payload.Name).IsEqualTo("Alice");
    }

    [Test]
    public void When_DeserializeDictionaryObjectPayloadWithUnknownAssemblyQualifiedType_Then_ThrowsJsonException()
    {
        var json = $$"""
        {
          "payload": {
            "capacity": 16,
            "_type": {{JsonString(UnsafeAssemblyQualifiedTypeAlias)}}
          }
        }
        """;

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<IDictionary<string, object>>(json, _options));
    }

    [Test]
    public async Task When_RegistryChangesAfterLegacyResolutionAttempt_Then_LegacyResolutionUsesCurrentRegistry()
    {
        var typeAlias = typeof(LateRegisteredPayload).GetSimpleAssemblyQualifiedName();
        await Assert.That(SerializationTypeResolver.TryResolveType(_workflowJsonTypeRegistry, typeAlias, out _)).IsFalse();

        _workflowJsonTypeRegistry.RegisterType(typeof(LateRegisteredPayload), "LateRegisteredPayload");

        await Assert.That(SerializationTypeResolver.TryResolveType(_workflowJsonTypeRegistry, typeAlias, out var result)).IsTrue();
        await Assert.That(result).IsEqualTo(typeof(LateRegisteredPayload));
    }

    private static JsonSerializerOptions CreateOptions(ISerializationTypeRegistry workflowJsonTypeRegistry) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PolymorphicObjectConverterFactory(workflowJsonTypeRegistry),
            new TypeJsonConverter(workflowJsonTypeRegistry)
        }
    };

    private static JsonSerializerOptions CreatePolymorphicOnlyOptions(ISerializationTypeRegistry workflowJsonTypeRegistry) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PolymorphicObjectConverterFactory(workflowJsonTypeRegistry)
        }
    };

    private static string JsonString(string value) => JsonSerializer.Serialize(value);

    public static IEnumerable<Func<(object Value, Type ExpectedType)>> JsonIslandValues()
    {
        yield return () => (new JObject { ["name"] = "Alice" }, typeof(JObject));
        yield return () => (new JArray("Alice", "Bob"), typeof(JArray));
        yield return () => (new JsonObject { ["name"] = "Alice" }, typeof(JsonObject));
        yield return () => (new JsonArray("Alice", "Bob"), typeof(JsonArray));
    }

    public sealed class RegisteredPayload
    {
        public string? Name { get; set; }
    }

    public sealed class UnregisteredPayload
    {
        public string? Name { get; set; }
    }

    public abstract class AbstractPayload
    {
        public string? Name { get; set; }
    }

    public sealed class LateRegisteredPayload
    {
        public string? Name { get; set; }
    }

    public sealed class ExpressionOnlyPayload
    {
        public string? Name { get; set; }
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
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
using Elsa.Common.Serialization;

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

    [Theory]
    [InlineData("String", typeof(string))]
    [InlineData("String[]", typeof(string[]))]
    [InlineData("String[][]", typeof(string[][]))]
    [InlineData("List<String>", typeof(List<string>))]
    [InlineData("List<String[]>", typeof(List<string[]>))]
    [InlineData("List<List<String>>", typeof(List<List<string>>))]
    [InlineData("ExceptionState", typeof(ExceptionState))]
    [InlineData("FaultException", typeof(FaultException))]
    [InlineData("ObjectDictionary", typeof(IDictionary<string, object>))]
    public void When_DeserializeRegisteredTypeAlias_Then_ReturnsExpectedType(string typeAlias, Type expectedType)
    {
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _options);

        Assert.Equal(expectedType, result);
    }

    [Fact]
    public void When_DeserializeRegisteredLegacyAssemblyQualifiedTypeAlias_Then_ReturnsExpectedType()
    {
        var typeAlias = typeof(RegisteredPayload).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonString(typeAlias), _options);

        Assert.Equal(typeof(RegisteredPayload), result);
    }

    [Fact]
    public void When_DeserializeRegisteredTypeAliasWithDifferentCasing_Then_ReturnsExpectedType()
    {
        var result = JsonSerializer.Deserialize<Type>(JsonString("registeredpayload"), _options);

        Assert.Equal(typeof(RegisteredPayload), result);
    }

    [Fact]
    public void When_DeserializeRegisteredLegacyGenericCollectionTypeAlias_Then_ReturnsExpectedType()
    {
        var typeAlias = typeof(List<RegisteredPayload>).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonString(typeAlias), _options);

        Assert.Equal(typeof(List<RegisteredPayload>), result);
    }

    [Theory]
    [InlineData(typeof(string), "String")]
    [InlineData(typeof(string[]), "String[]")]
    [InlineData(typeof(string[][]), "String[][]")]
    [InlineData(typeof(List<string>), "List<String>")]
    [InlineData(typeof(List<string[]>), "List<String[]>")]
    [InlineData(typeof(List<List<string>>), "List<List<String>>")]
    [InlineData(typeof(ExceptionState), "ExceptionState")]
    [InlineData(typeof(FaultException), "FaultException")]
    public void When_SerializeSupportedType_Then_EmitsAliasThatCanBeDeserialized(Type type, string expectedAlias)
    {
        var json = JsonSerializer.Serialize(type, _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Equal(expectedAlias, alias);
        Assert.Equal(type, result);
    }

    [Theory]
    [InlineData(typeof(IEnumerable<string>), "List<String>", typeof(List<string>))]
    [InlineData(typeof(ICollection<string>), "List<String>", typeof(List<string>))]
    [InlineData(typeof(IList<string>), "List<String>", typeof(List<string>))]
    [InlineData(typeof(IReadOnlyCollection<string>), "List<String>", typeof(List<string>))]
    [InlineData(typeof(IReadOnlyList<string>), "List<String>", typeof(List<string>))]
    [InlineData(typeof(ISet<string>), "HashSet<String>", typeof(HashSet<string>))]
    public void When_SerializeInterfaceCollectionType_Then_EmitsInstantiableAlias(Type type, string expectedAlias, Type expectedRoundTripType)
    {
        var json = JsonSerializer.Serialize(type, _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Equal(expectedAlias, alias);
        Assert.Equal(expectedRoundTripType, result);
    }

    [Theory]
    [MemberData(nameof(JsonIslandValues))]
    public void When_SerializeSpecialJsonIslandType_Then_CanBeDeserialized(object value, Type expectedType)
    {
        _workflowJsonTypeRegistry.RegisterType(typeof(JObject), nameof(JObject));
        _workflowJsonTypeRegistry.RegisterType(typeof(JArray), nameof(JArray));

        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<object>(json, _options);

        Assert.IsType(expectedType, result);
    }

    [Theory]
    [InlineData(typeof(System.Text.StringBuilder))]
    [InlineData(typeof(System.Text.StringBuilder[]))]
    [InlineData(typeof(List<System.Text.StringBuilder>))]
    public void When_SerializeUnsupportedType_Then_EmitsSafeUnregisteredTypeAlias(Type type)
    {
        var json = JsonSerializer.Serialize(type, _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.StartsWith("UnregisteredClrType:", alias);
        Assert.Equal(typeof(Exception), result);
    }

    [Fact]
    public void When_SerializeExceptionStateWithUnregisteredExceptionType_Then_DoesNotThrow()
    {
        var exceptionState = ExceptionState.FromException(new NullReferenceException("Test"));

        var json = JsonSerializer.Serialize(exceptionState, _options);
        var result = JsonSerializer.Deserialize<ExceptionState>(json, _options)!;

        Assert.Contains("UnregisteredClrType:", json);
        Assert.Equal(typeof(Exception), result.Type);
        Assert.Equal("Test", result.Message);
    }

    [Fact]
    public void When_ConfigureWorkflowsFeature_Then_RegistersCoreAliases()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.UseWorkflows();
        module.Apply();
        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<ISerializationTypeRegistry>();

        var aliasRegistered = registry.TryGetAlias(typeof(NullReferenceException), out var alias);
        var typeRegistered = registry.TryGetType(nameof(NullReferenceException), out var type);

        Assert.True(aliasRegistered);
        Assert.Equal(nameof(NullReferenceException), alias);
        Assert.True(typeRegistered);
        Assert.Equal(typeof(NullReferenceException), type);
        Assert.True(registry.TryGetAlias(typeof(MemoryStorageDriver), out var memoryStorageDriverAlias));
        Assert.Equal(nameof(MemoryStorageDriver), memoryStorageDriverAlias);
        Assert.True(registry.TryGetType(nameof(MemoryStorageDriver), out var memoryStorageDriverType));
        Assert.Equal(typeof(MemoryStorageDriver), memoryStorageDriverType);
        Assert.True(registry.TryGetType(typeof(MemoryStorageDriver).GetSimpleAssemblyQualifiedName(), out var legacyMemoryStorageDriverType));
        Assert.Equal(typeof(MemoryStorageDriver), legacyMemoryStorageDriverType);
        Assert.True(registry.TryGetAlias(typeof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy), out var incidentStrategyAlias));
        Assert.Equal(nameof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy), incidentStrategyAlias);
        Assert.True(registry.TryGetType(typeof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy).GetSimpleAssemblyQualifiedName(), out var legacyIncidentStrategyType));
        Assert.Equal(typeof(Elsa.Workflows.IncidentStrategies.ContinueWithIncidentsStrategy), legacyIncidentStrategyType);
    }

    [Fact]
    public void When_TypeAliasExistsOnlyInExpressionOptions_Then_WorkflowJsonDoesNotResolveIt()
    {
        var expressionOptions = new ExpressionOptions();
        expressionOptions.RegisterTypeAlias(typeof(ExpressionOnlyPayload), "ExpressionOnlyPayload");
        var expressionRegistry = new WellKnownTypeRegistry(Microsoft.Extensions.Options.Options.Create(expressionOptions));

        Assert.True(expressionRegistry.TryGetType("ExpressionOnlyPayload", out _));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonString("ExpressionOnlyPayload"), _options));
    }

    [Fact]
    public void When_SerializePolymorphicObjectWithUnregisteredType_Then_OmitsTypeMetadata()
    {
        var json = JsonSerializer.Serialize<object>(new UnregisteredPayload { Name = "Alice" }, _options);

        var result = JsonSerializer.Deserialize<object>(json, _options);

        Assert.DoesNotContain("\"_type\"", json);
        var payload = Assert.IsAssignableFrom<IDictionary<string, object>>(result);
        Assert.Equal("Alice", payload["name"]);
    }

    [Fact]
    public void When_DeserializeUnknownAssemblyQualifiedTypeAlias_Then_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonString(UnsafeAssemblyQualifiedTypeAlias), _options));
    }

    [Fact]
    public void When_DeserializeUnknownGenericElementTypeAlias_Then_ThrowsJsonException()
    {
        var typeAlias = $"List<{UnsafeAssemblyQualifiedTypeAlias}>";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonString(typeAlias), _options));
    }

    [Fact]
    public void When_DeserializePolymorphicObjectWithRegisteredTypeAlias_Then_ReturnsTypedObject()
    {
        var json = """
        {
          "name": "Alice",
          "_type": "RegisteredPayload"
        }
        """;

        var result = JsonSerializer.Deserialize<object>(json, _options);

        var payload = Assert.IsType<RegisteredPayload>(result);
        Assert.Equal("Alice", payload.Name);
    }

    [Theory]
    [InlineData("IEnumerable<String>", typeof(List<string>))]
    [InlineData("ICollection<String>", typeof(List<string>))]
    [InlineData("IList<String>", typeof(List<string>))]
    [InlineData("IReadOnlyCollection<String>", typeof(List<string>))]
    [InlineData("IReadOnlyList<String>", typeof(List<string>))]
    [InlineData("ISet<String>", typeof(HashSet<string>))]
    public void When_DeserializePolymorphicCollectionInterface_Then_ReturnsConcreteCollection(string typeAlias, Type expectedType)
    {
        var json = $$"""
        {
          "_items": ["Alice"],
          "_type": "{{typeAlias}}"
        }
        """;

        var result = JsonSerializer.Deserialize<object>(json, _options);

        Assert.IsType(expectedType, result);
    }

    [Fact]
    public void When_DeserializePolymorphicObjectWithNonInstantiableType_Then_ThrowsJsonException()
    {
        _workflowJsonTypeRegistry.RegisterType(typeof(AbstractPayload), "AbstractPayload");
        var json = """
        {
          "_type": "AbstractPayload"
        }
        """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(json, _options));
    }

    [Fact]
    public void When_DeserializePolymorphicObjectWithUnknownAssemblyQualifiedType_Then_ThrowsJsonException()
    {
        var json = $$"""
        {
          "capacity": 16,
          "_type": {{JsonString(UnsafeAssemblyQualifiedTypeAlias)}}
        }
        """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(json, _options));
    }

    [Fact]
    public void When_DeserializePolymorphicObjectWithoutTypeJsonConverterAndUnknownAssemblyQualifiedType_Then_ThrowsJsonException()
    {
        var options = CreatePolymorphicOnlyOptions(_workflowJsonTypeRegistry);
        var json = $$"""
        {
          "capacity": 16,
          "_type": {{JsonString(UnsafeAssemblyQualifiedTypeAlias)}}
        }
        """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(json, options));
    }

    [Fact]
    public void When_DeserializeDictionaryObjectPayloadWithRegisteredTypeAlias_Then_ReturnsTypedObjectValue()
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

        var payload = Assert.IsType<RegisteredPayload>(result["payload"]);
        Assert.Equal("Alice", payload.Name);
    }

    [Fact]
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

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IDictionary<string, object>>(json, _options));
    }

    [Fact]
    public void When_RegistryChangesAfterLegacyResolutionAttempt_Then_LegacyResolutionUsesCurrentRegistry()
    {
        var typeAlias = typeof(LateRegisteredPayload).GetSimpleAssemblyQualifiedName();
        Assert.False(SerializationTypeResolver.TryResolveType(_workflowJsonTypeRegistry, typeAlias, out _));

        _workflowJsonTypeRegistry.RegisterType(typeof(LateRegisteredPayload), "LateRegisteredPayload");

        Assert.True(SerializationTypeResolver.TryResolveType(_workflowJsonTypeRegistry, typeAlias, out var result));
        Assert.Equal(typeof(LateRegisteredPayload), result);
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

    public static TheoryData<object, Type> JsonIslandValues() => new()
    {
        { new JObject { ["name"] = "Alice" }, typeof(JObject) },
        { new JArray("Alice", "Bob"), typeof(JArray) },
        { new JsonObject { ["name"] = "Alice" }, typeof(JsonObject) },
        { new JsonArray("Alice", "Bob"), typeof(JsonArray) }
    };

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

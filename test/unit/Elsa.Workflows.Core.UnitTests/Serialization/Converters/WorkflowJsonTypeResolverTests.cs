using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.State;
using Newtonsoft.Json.Linq;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public sealed class WorkflowJsonTypeResolverTests
{
    private static readonly string UnsafeAssemblyQualifiedTypeAlias = typeof(System.Text.StringBuilder).AssemblyQualifiedName!;
    private readonly WellKnownTypeRegistry _wellKnownTypeRegistry = new(Microsoft.Extensions.Options.Options.Create(new ExpressionOptions()));
    private readonly JsonSerializerOptions _options;

    public WorkflowJsonTypeResolverTests()
    {
        _wellKnownTypeRegistry.RegisterType(typeof(ExceptionState), nameof(ExceptionState));
        _wellKnownTypeRegistry.RegisterType(typeof(FaultException), nameof(FaultException));
        _wellKnownTypeRegistry.RegisterType(typeof(RegisteredPayload), "RegisteredPayload");
        _options = CreateOptions(_wellKnownTypeRegistry);
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
        _wellKnownTypeRegistry.RegisterType(typeof(JObject), nameof(JObject));
        _wellKnownTypeRegistry.RegisterType(typeof(JArray), nameof(JArray));

        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<object>(json, _options);

        Assert.IsType(expectedType, result);
    }

    [Theory]
    [InlineData(typeof(System.Text.StringBuilder))]
    [InlineData(typeof(System.Text.StringBuilder[]))]
    [InlineData(typeof(List<System.Text.StringBuilder>))]
    public void When_SerializeUnsupportedType_Then_ThrowsJsonException(Type type)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(type, _options));
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
        var options = CreatePolymorphicOnlyOptions(_wellKnownTypeRegistry);
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

    private static JsonSerializerOptions CreateOptions(WellKnownTypeRegistry wellKnownTypeRegistry) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PolymorphicObjectConverterFactory(wellKnownTypeRegistry),
            new TypeJsonConverter(wellKnownTypeRegistry)
        }
    };

    private static JsonSerializerOptions CreatePolymorphicOnlyOptions(WellKnownTypeRegistry wellKnownTypeRegistry) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PolymorphicObjectConverterFactory(wellKnownTypeRegistry)
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
}

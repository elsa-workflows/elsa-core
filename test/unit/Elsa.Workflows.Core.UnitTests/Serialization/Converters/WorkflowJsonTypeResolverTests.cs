using System.Text.Json;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public sealed class WorkflowJsonTypeResolverTests
{
    private static readonly string UnsafeAssemblyQualifiedTypeAlias = typeof(System.Text.StringBuilder).AssemblyQualifiedName!;
    private readonly WellKnownTypeRegistry _wellKnownTypeRegistry = new(Microsoft.Extensions.Options.Options.Create(new ExpressionOptions()));
    private readonly JsonSerializerOptions _options;

    public WorkflowJsonTypeResolverTests()
    {
        _wellKnownTypeRegistry.RegisterType(typeof(RegisteredPayload), "RegisteredPayload");
        _options = CreateOptions(_wellKnownTypeRegistry);
    }

    [Theory]
    [InlineData("String", typeof(string))]
    [InlineData("String[]", typeof(string[]))]
    [InlineData("List<String>", typeof(List<string>))]
    [InlineData("ObjectDictionary", typeof(IDictionary<string, object>))]
    public void When_DeserializeRegisteredTypeAlias_Then_ReturnsExpectedType(string typeAlias, Type expectedType)
    {
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _options);

        Assert.Equal(expectedType, result);
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

    public sealed class RegisteredPayload
    {
        public string? Name { get; set; }
    }
}

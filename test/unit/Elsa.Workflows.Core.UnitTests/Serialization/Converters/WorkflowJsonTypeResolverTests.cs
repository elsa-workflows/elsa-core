using System.Text.Json;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public class WorkflowJsonTypeResolverTests
{
    private static readonly string UnsafeAssemblyQualifiedTypeAlias = typeof(System.Text.StringBuilder).AssemblyQualifiedName!;
    private readonly WellKnownTypeRegistry _registry = new(Microsoft.Extensions.Options.Options.Create(new ExpressionOptions()));
    private readonly JsonSerializerOptions _options;

    public WorkflowJsonTypeResolverTests()
    {
        _registry.RegisterType(typeof(RegisteredPayload), "RegisteredPayload");
        _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new PolymorphicObjectConverterFactory(_registry),
                new TypeJsonConverter(_registry)
            }
        };
    }

    [Fact]
    public void TypeJsonConverter_DoesNotLoadUnknownAssemblyQualifiedType()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(UnsafeAssemblyQualifiedTypeAlias), _options));
    }

    [Fact]
    public void PolymorphicObjectConverter_DoesNotLoadUnknownAssemblyQualifiedType()
    {
        var json = $$"""
        {
          "capacity": 16,
          "_type": {{JsonSerializer.Serialize(UnsafeAssemblyQualifiedTypeAlias)}}
        }
        """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(json, _options));
    }

    [Fact]
    public void PolymorphicObjectConverter_ResolvesRegisteredTypeAlias()
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
    public void PolymorphicObjectConverter_OmitsTypeMetadataForUnregisteredTypes()
    {
        var json = JsonSerializer.Serialize<object>(new UnregisteredPayload { Name = "Alice" }, _options);

        Assert.DoesNotContain("\"_type\"", json);
    }

    [Fact]
    public void TypeJsonConverter_WritesUnregisteredTypesAsMetadataOnly()
    {
        var json = JsonSerializer.Serialize(typeof(System.Text.StringBuilder), _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.StartsWith("UnregisteredClrType:", alias);
        Assert.Equal(typeof(Exception), result);
    }

    [Fact]
    public void TypeJsonConverter_ResolvesRegisteredLegacyAssemblyQualifiedName()
    {
        var typeAlias = typeof(RegisteredPayload).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _options);

        Assert.Equal(typeof(RegisteredPayload), result);
    }

    public sealed class RegisteredPayload
    {
        public string? Name { get; set; }
    }

    public sealed class UnregisteredPayload
    {
        public string? Name { get; set; }
    }
}

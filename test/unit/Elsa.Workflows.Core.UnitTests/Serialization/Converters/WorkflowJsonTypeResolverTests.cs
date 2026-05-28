using System.Text.Json;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.IncidentStrategies;
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
    public void TypeJsonConverter_ResolvesClrAssemblyQualifiedType()
    {
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(UnsafeAssemblyQualifiedTypeAlias), _options);

        Assert.Equal(typeof(System.Text.StringBuilder), result);
    }

    [Fact]
    public void TypeJsonConverter_ResolvesIncidentStrategyClrType()
    {
        var typeAlias = typeof(ContinueWithIncidentsStrategy).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _options);

        Assert.Equal(typeof(ContinueWithIncidentsStrategy), result);
    }

    [Fact]
    public void PolymorphicObjectConverter_ResolvesClrAssemblyQualifiedType()
    {
        var json = $$"""
        {
          "name": "Alice",
          "_type": {{JsonSerializer.Serialize(typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName())}}
        }
        """;

        var result = JsonSerializer.Deserialize<object>(json, _options);

        var payload = Assert.IsType<UnregisteredPayload>(result);
        Assert.Equal("Alice", payload.Name);
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
    public void PolymorphicObjectConverter_WritesClrTypeMetadataForUnregisteredTypes()
    {
        var json = JsonSerializer.Serialize<object>(new UnregisteredPayload { Name = "Alice" }, _options);
        var result = JsonSerializer.Deserialize<object>(json, _options);

        Assert.Contains("\"_type\"", json);
        var payload = Assert.IsType<UnregisteredPayload>(result);
        Assert.Equal("Alice", payload.Name);
    }

    [Fact]
    public void TypeJsonConverter_WritesUnregisteredTypesAsClrTypeNames()
    {
        var json = JsonSerializer.Serialize(typeof(System.Text.StringBuilder), _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Equal(typeof(System.Text.StringBuilder).GetSimpleAssemblyQualifiedName(), alias);
        Assert.Equal(typeof(System.Text.StringBuilder), result);
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

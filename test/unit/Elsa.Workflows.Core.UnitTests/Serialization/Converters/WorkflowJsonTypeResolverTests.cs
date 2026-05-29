using System.Text.Json;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public class WorkflowJsonTypeResolverTests
{
    private static readonly string UnsafeAssemblyQualifiedTypeAlias = typeof(System.Text.StringBuilder).AssemblyQualifiedName!;
    private static readonly string TrustedAssemblyQualifiedTypeAlias = typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName();
    private readonly WellKnownTypeRegistry _registry = new(Microsoft.Extensions.Options.Options.Create(new ExpressionOptions()));
    private readonly JsonSerializerOptions _options;
    private readonly JsonSerializerOptions _strictOptions;

    public WorkflowJsonTypeResolverTests()
    {
        _registry.RegisterType(typeof(RegisteredPayload), "RegisteredPayload");
        _registry.RegisterType(typeof(FaultStrategy), nameof(FaultStrategy));
        _options = CreateOptions();
        _strictOptions = CreateOptions(false);
    }

    [Fact]
    public void TypeJsonConverter_ResolvesClrAssemblyQualifiedType()
    {
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(UnsafeAssemblyQualifiedTypeAlias), _options);

        Assert.Equal(typeof(System.Text.StringBuilder), result);
    }

    [Fact]
    public void TypeJsonConverter_RejectsUnregisteredClrAssemblyQualifiedType_WhenLegacyClrTypeNamesDisabled()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(UnsafeAssemblyQualifiedTypeAlias), _strictOptions));
    }

    [Fact]
    public void TypeJsonConverter_ResolvesTrustedClrAssemblyQualifiedType()
    {
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(TrustedAssemblyQualifiedTypeAlias), _options);

        Assert.Equal(typeof(UnregisteredPayload), result);
    }

    [Fact]
    public void TypeJsonConverter_ResolvesIncidentStrategyClrType()
    {
        var typeAlias = typeof(ContinueWithIncidentsStrategy).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _options);

        Assert.Equal(typeof(ContinueWithIncidentsStrategy), result);
    }

    [Fact]
    public void TypeJsonConverter_ResolvesFaultExceptionClrType()
    {
        var typeAlias = typeof(FaultException).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _options);

        Assert.Equal(typeof(FaultException), result);
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
    public void PolymorphicObjectConverter_RejectsUnregisteredClrAssemblyQualifiedType_WhenLegacyClrTypeNamesDisabled()
    {
        var json = $$"""
        {
          "name": "Alice",
          "_type": {{JsonSerializer.Serialize(typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName())}}
        }
        """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(json, _strictOptions));
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
        var json = JsonSerializer.Serialize(typeof(UnregisteredPayload), _options);
        var alias = JsonSerializer.Deserialize<string>(json);
        var result = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Equal(typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), alias);
        Assert.Equal(typeof(UnregisteredPayload), result);
    }

    [Fact]
    public void TypeJsonConverter_ResolvesRegisteredLegacyAssemblyQualifiedName()
    {
        var typeAlias = typeof(RegisteredPayload).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _strictOptions);

        Assert.Equal(typeof(RegisteredPayload), result);
    }

    private JsonSerializerOptions CreateOptions(bool allowLegacyClrTypeNames = true) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PolymorphicObjectConverterFactory(_registry, allowLegacyClrTypeNames),
            new TypeJsonConverter(_registry, allowLegacyClrTypeNames)
        }
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

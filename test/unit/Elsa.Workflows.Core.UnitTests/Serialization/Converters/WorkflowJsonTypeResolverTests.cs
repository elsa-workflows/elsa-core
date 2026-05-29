using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using Elsa.Workflows.Serialization.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public class WorkflowJsonTypeResolverTests
{
    private static readonly string UnsafeAssemblyQualifiedTypeAlias = typeof(System.Text.StringBuilder).AssemblyQualifiedName!;
    private static readonly string TrustedAssemblyQualifiedTypeAlias = typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName();
    private readonly JsonSerializerOptions _options;
    private readonly JsonSerializerOptions _strictOptions;

    public WorkflowJsonTypeResolverTests()
    {
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
    public void PolymorphicObjectConverter_RejectsUnregisteredClrTypeMetadata_WhenLegacyClrTypeNamesDisabled()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize<object>(new UnregisteredPayload { Name = "Alice" }, _strictOptions));
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
    public void TypeJsonConverter_RejectsUnregisteredClrTypeMetadata_WhenLegacyClrTypeNamesDisabled()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(typeof(UnregisteredPayload), _strictOptions));
    }

    [Fact]
    public void TypeJsonConverter_ResolvesRegisteredLegacyAssemblyQualifiedName()
    {
        var typeAlias = typeof(RegisteredPayload).GetSimpleAssemblyQualifiedName();

        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize(typeAlias), _strictOptions);

        Assert.Equal(typeof(RegisteredPayload), result);
    }

    [Fact]
    public void TypeJsonConverter_ResolvesRegisteredAliasesCaseInsensitively()
    {
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize("registeredpayload"), _strictOptions);

        Assert.Equal(typeof(RegisteredPayload), result);
    }

    [Fact]
    public void TypeJsonConverter_WritesPreferredAlias_WhenTypeHasMultipleAliases()
    {
        var json = JsonSerializer.Serialize(typeof(FlowJoinMode), _strictOptions);

        Assert.Equal("\"FlowJoinMode\"", json);
    }

    [Fact]
    public void TypeJsonConverter_RoundTripsNullableValueTypeAliases_WhenLegacyClrTypeNamesDisabled()
    {
        var json = JsonSerializer.Serialize(typeof(int?), _strictOptions);
        var result = JsonSerializer.Deserialize<Type>(JsonSerializer.Serialize("int32?"), _strictOptions);

        Assert.Equal("\"Int32?\"", json);
        Assert.Equal(typeof(int?), result);
    }

    [Fact]
    public void ShellWorkflowsFeature_RegistersWorkflowJsonTypeAliases()
    {
        var services = new ServiceCollection();
        new Elsa.Workflows.ShellFeatures.WorkflowsFeature().ConfigureServices(services);
        var workflowJsonOptions = services.BuildServiceProvider().GetRequiredService<IOptions<WorkflowJsonOptions>>().Value;

        var faultStrategy = WorkflowJsonTypeResolver.ResolveType(workflowJsonOptions, nameof(FaultStrategy), false);

        Assert.Equal(typeof(FaultStrategy), faultStrategy);
    }

    [Fact]
    public void ShellFlowchartFeature_RegistersFlowScopeTypeAlias()
    {
        var services = new ServiceCollection();
        new Elsa.Workflows.ShellFeatures.FlowchartFeature().ConfigureServices(services);
        var workflowJsonOptions = services.BuildServiceProvider().GetRequiredService<IOptions<WorkflowJsonOptions>>().Value;

        var flowScope = WorkflowJsonTypeResolver.ResolveType(workflowJsonOptions, "FlowScope", false);

        Assert.Equal(typeof(FlowScope), flowScope);
    }

    private JsonSerializerOptions CreateOptions(bool allowLegacyClrTypeNames = true)
    {
        var workflowJsonOptions = new WorkflowJsonOptions
        {
            AllowLegacyClrTypeNames = allowLegacyClrTypeNames
        };
        workflowJsonOptions.RegisterWorkflowTypeAliases();
        workflowJsonOptions.RegisterTypeAlias(typeof(RegisteredPayload), "RegisteredPayload");
        workflowJsonOptions.RegisterTypeAlias(typeof(FaultStrategy), nameof(FaultStrategy));

        return new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PolymorphicObjectConverterFactory(workflowJsonOptions),
            new TypeJsonConverter(workflowJsonOptions)
        }
    };
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

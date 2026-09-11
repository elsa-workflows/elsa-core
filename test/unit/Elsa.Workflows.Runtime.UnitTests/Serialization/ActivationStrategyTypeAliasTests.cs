using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Services;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Runtime.UnitTests.Serialization;

public class ActivationStrategyTypeAliasTests
{
    public static TheoryData<Type, string> BuiltInActivationStrategies() => new()
    {
        { typeof(SingletonStrategy), nameof(SingletonStrategy) },
        { typeof(CorrelatedSingletonStrategy), nameof(CorrelatedSingletonStrategy) },
        { typeof(CorrelationStrategy), nameof(CorrelationStrategy) }
    };

    [Theory]
    [MemberData(nameof(BuiltInActivationStrategies))]
    public void When_RegisterRuntimeAliases_Then_ResolvesPreferredAndLegacyNames(Type strategyType, string alias)
    {
        var registry = CreateRegistry();

        Assert.True(registry.TryGetAlias(strategyType, out var registeredAlias));
        Assert.Equal(alias, registeredAlias);
        Assert.True(registry.TryGetType(alias, out var byAlias));
        Assert.Equal(strategyType, byAlias);
        Assert.True(registry.TryGetType(strategyType.GetSimpleAssemblyQualifiedName(), out var byLegacyName));
        Assert.Equal(strategyType, byLegacyName);
    }

    [Theory]
    [MemberData(nameof(BuiltInActivationStrategies))]
    public void When_DeserializeWorkflowOptions_Then_ResolvesActivationStrategyType(Type strategyType, string alias)
    {
        var options = CreateJsonOptions();

        var byAlias = JsonSerializer.Deserialize<WorkflowOptions>($$"""{"activationStrategyType":{{JsonSerializer.Serialize(alias)}}}""", options);
        Assert.Equal(strategyType, byAlias!.ActivationStrategyType);

        var byLegacyName = JsonSerializer.Deserialize<WorkflowOptions>($$"""{"activationStrategyType":{{JsonSerializer.Serialize(strategyType.GetSimpleAssemblyQualifiedName())}}}""", options);
        Assert.Equal(strategyType, byLegacyName!.ActivationStrategyType);

        var serialized = JsonSerializer.Serialize(new WorkflowOptions { ActivationStrategyType = strategyType }, options);
        Assert.Contains(alias, serialized);
        Assert.DoesNotContain("UnregisteredClrType:", serialized);

        var roundTrip = JsonSerializer.Deserialize<WorkflowOptions>(serialized, options);
        Assert.Equal(strategyType, roundTrip!.ActivationStrategyType);
    }

    private static ISerializationTypeRegistry CreateRegistry()
    {
        var options = new SerializationTypeOptions();
        WorkflowRuntimeTypeAliasRegistrar.Register(options, []);
        return new SerializationTypeRegistry(Microsoft.Extensions.Options.Options.Create(options));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var registry = CreateRegistry();
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new TypeJsonConverter(registry) }
        };
    }
}

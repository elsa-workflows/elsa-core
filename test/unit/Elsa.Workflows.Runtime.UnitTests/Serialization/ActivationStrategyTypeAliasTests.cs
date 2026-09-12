using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.ActivationValidators;

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
        var registry = ActivationStrategyAliasTestHelpers.CreateRegistry();

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
        var options = ActivationStrategyAliasTestHelpers.CreateJsonOptions();

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
}

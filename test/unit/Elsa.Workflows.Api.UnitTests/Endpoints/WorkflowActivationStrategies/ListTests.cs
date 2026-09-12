using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Api.Endpoints.WorkflowActivationStrategies.List;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowActivationStrategies;

public class ListTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsWorkflowJsonTypeIdentifier_ForActivationStrategyTypeName()
    {
        var registry = SerializationTypeTestHelpers.CreateRegistry(options =>
            options.AddTypeAliasWithLegacyName<AllowAlwaysStrategy>(nameof(AllowAlwaysStrategy)));
        var endpoint = new List([new AllowAlwaysStrategy()], registry);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        var descriptor = Assert.Single(response.Items);
        Assert.Equal(nameof(AllowAlwaysStrategy), descriptor.TypeName);
        Assert.True(registry.TryGetType(typeof(AllowAlwaysStrategy).GetSimpleAssemblyQualifiedName(), out var legacyType));
        Assert.Equal(typeof(AllowAlwaysStrategy), legacyType);
    }
}

using Elsa.Extensions;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Api.Endpoints.WorkflowActivationStrategies.List;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowActivationStrategies;

public class ListTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsWorkflowJsonTypeIdentifier_ForActivationStrategyTypeName()
    {
        var options = new SerializationTypeOptions();
        options.RegisterTypeAlias(typeof(AllowAlwaysStrategy), nameof(AllowAlwaysStrategy));
        options.RegisterLegacySimpleAssemblyQualifiedName(typeof(AllowAlwaysStrategy));
        var registry = new SerializationTypeRegistry(Microsoft.Extensions.Options.Options.Create(options));
        var endpoint = new List([new AllowAlwaysStrategy()], registry);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        var descriptor = Assert.Single(response.Items);
        Assert.Equal(nameof(AllowAlwaysStrategy), descriptor.TypeName);
        Assert.True(registry.TryGetType(typeof(AllowAlwaysStrategy).GetSimpleAssemblyQualifiedName(), out var legacyType));
        Assert.Equal(typeof(AllowAlwaysStrategy), legacyType);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOnlyLegacyNameIsRegistered_AdvertisesResolvableIdentifier()
    {
        var options = new SerializationTypeOptions();
        options.RegisterLegacySimpleAssemblyQualifiedName(typeof(AllowAlwaysStrategy));
        var registry = new SerializationTypeRegistry(Microsoft.Extensions.Options.Options.Create(options));
        var endpoint = new List([new AllowAlwaysStrategy()], registry);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        var descriptor = Assert.Single(response.Items);
        var expectedTypeName = typeof(AllowAlwaysStrategy).GetSimpleAssemblyQualifiedName();
        Assert.Equal(expectedTypeName, descriptor.TypeName);
        Assert.True(registry.TryGetType(descriptor.TypeName, out var resolvedType));
        Assert.Equal(typeof(AllowAlwaysStrategy), resolvedType);
    }
}

using Elsa.Extensions;
using Elsa.Workflows.Api.Endpoints.IncidentStrategies.List;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.IncidentStrategies;

public class ListTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsWorkflowJsonTypeIdentifier_ForIncidentStrategyTypeName()
    {
        var options = new WorkflowJsonTypeOptions();
        options.RegisterTypeAlias(typeof(ContinueWithIncidentsStrategy), nameof(ContinueWithIncidentsStrategy));
        options.RegisterLegacySimpleAssemblyQualifiedName(typeof(ContinueWithIncidentsStrategy));
        var registry = new WorkflowJsonTypeRegistry(Microsoft.Extensions.Options.Options.Create(options));
        var endpoint = new List([new ContinueWithIncidentsStrategy()], registry);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        var descriptor = Assert.Single(response.Items);
        Assert.Equal(nameof(ContinueWithIncidentsStrategy), descriptor.TypeName);
        Assert.True(registry.TryGetType(typeof(ContinueWithIncidentsStrategy).GetSimpleAssemblyQualifiedName(), out var legacyType));
        Assert.Equal(typeof(ContinueWithIncidentsStrategy), legacyType);
    }
}

using Elsa.UnitTests.Shared;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;
using Elsa.Workflows.Api.UnitTests.Endpoints.Descriptors;
using Elsa.Workflows.Api.UnitTests.Endpoints.Features;
using Elsa.Workflows.Management;
using FastEndpoints;
using NSubstitute;

namespace Elsa.Workflows.Api.UnitTests.Endpoints;

/// <summary>
/// Pins the cleanup the classes in <see cref="FastEndpointsCollection"/> depend on, in the one order that used to fail:
/// an endpoint host runs and is disposed, and only then is an endpoint created through the factory.
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public class FastEndpointsResolverTests
{
    [Theory]
    [InlineData(typeof(DescriptorCatalogAuthorizationTests))]
    [InlineData(typeof(InstalledFeaturesAuthorizationTests))]
    public async Task FactoryCreatesEndpointsAfterAnEndpointHostIsDisposed(Type hostTestClass)
    {
        var host = (IAsyncLifetime)Activator.CreateInstance(hostTestClass)!;
        await host.InitializeAsync();
        await host.DisposeAsync();

        Assert.NotNull(Factory.Create<List>(Substitute.For<IWorkflowDefinitionStore>(), Substitute.For<IWorkflowDefinitionLinker>(), Array.Empty<IWorkflowDefinitionFilterProvider>()));
    }
}

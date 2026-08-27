using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Interchange;

/// <summary>
/// The application the Analyze/Import/Export endpoints' shared service is exercised in.
/// </summary>
public abstract class BpmnInterchangeTestBase : IAsyncLifetime
{
    private readonly IServiceProvider _services;

    protected BpmnInterchangeTestBase(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();

        DocumentService = _services.GetRequiredService<BpmnInterchangeDocumentService>();
        DefinitionStore = _services.GetRequiredService<IWorkflowDefinitionStore>();
    }

    protected BpmnInterchangeDocumentService DocumentService { get; }

    protected IWorkflowDefinitionStore DefinitionStore { get; }

    public Task InitializeAsync() => _services.PopulateRegistriesAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Reads a fixture from the <c>Assets</c> directory shipped alongside this test project.</summary>
    protected static string ReadAsset(string fileName) => BpmnAssetReader.Read(fileName);
}

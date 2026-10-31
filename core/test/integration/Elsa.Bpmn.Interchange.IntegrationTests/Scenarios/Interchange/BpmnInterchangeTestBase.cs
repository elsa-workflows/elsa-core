using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
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
        DefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        ActivitySerializer = _services.GetRequiredService<IActivitySerializer>();
    }

    protected BpmnInterchangeDocumentService DocumentService { get; }

    protected IWorkflowDefinitionStore DefinitionStore { get; }

    /// <summary>
    /// Resolves the same <see cref="IWorkflowDefinitionPublisher"/> the workflow-definition save endpoint the
    /// designer uses calls, so a test can save a draft the way the designer does:
    /// <see cref="IWorkflowDefinitionPublisher.GetDraftAsync(string, Elsa.Common.Models.VersionOptions, CancellationToken)"/>
    /// then <see cref="IWorkflowDefinitionPublisher.SaveDraftAsync"/>.
    /// </summary>
    protected IWorkflowDefinitionPublisher DefinitionPublisher { get; }

    /// <summary>Resolves activities out of a draft's <c>StringData</c> so a test can edit one before saving it back.</summary>
    protected IActivitySerializer ActivitySerializer { get; }

    public Task InitializeAsync() => _services.PopulateRegistriesAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Reads a fixture from the <c>Assets</c> directory shipped alongside this test project.</summary>
    protected static string ReadAsset(string fileName) => BpmnAssetReader.Read(fileName);
}

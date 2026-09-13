using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Publishing;

/// <summary>
/// The application <c>ValidateBpmnProcessBindings</c> — the publish-time second net for an unbound task, alongside
/// <see cref="BpmnWorkBinder"/>'s own bind-time refusal — is exercised in.
/// </summary>
/// <remarks>
/// Derives from <see cref="BpmnBindingTestBase"/> rather than duplicating its container setup and its <c>Ref</c>/
/// <c>BoundElement</c> helpers: both suites exercise the same binder and format over the same kind of application,
/// and a definition that publishes here was, in the "edited after import" cases, produced by that same binder.
/// </remarks>
public abstract class BpmnPublishGateTestBase : BpmnBindingTestBase
{
    protected BpmnPublishGateTestBase()
    {
        DocumentService = Services.GetRequiredService<BpmnInterchangeDocumentService>();
        Importer = Services.GetRequiredService<IWorkflowDefinitionImporter>();
        Publisher = Services.GetRequiredService<IWorkflowDefinitionPublisher>();
        DefinitionService = Services.GetRequiredService<IWorkflowDefinitionService>();
    }

    protected BpmnInterchangeDocumentService DocumentService { get; }

    protected IWorkflowDefinitionImporter Importer { get; }

    protected IWorkflowDefinitionPublisher Publisher { get; }

    protected IWorkflowDefinitionService DefinitionService { get; }

    /// <summary>Reads a fixture from the <c>Assets</c> directory shipped alongside this test project.</summary>
    protected static string ReadAsset(string fileName) => BpmnAssetReader.Read(fileName);

    /// <summary>
    /// Saves <paramref name="root"/> as an unpublished draft, exactly as an author's save does. Draft saves never
    /// validate — see <c>WorkflowDefinitionImporter</c> — so a draft that fails to publish must fail at the explicit
    /// <see cref="PublishAsync"/> call below, not here.
    /// </summary>
    protected async Task<string> SaveDraftAsync(IActivity root, string? definitionId = null)
    {
        var result = await Importer.ImportAsync(new()
        {
            Model = new WorkflowDefinitionModel { DefinitionId = definitionId ?? string.Empty, Name = "Publish gate fixture", Root = root },
            Publish = false
        });

        await Assert.That(result.Succeeded).IsTrue()
            .Because(string.Join("; ", result.ValidationErrors.Select(error => error.Message)));

        return result.WorkflowDefinition.DefinitionId;
    }

    /// <summary>Publishes the latest draft of <paramref name="definitionId"/>, the same call the Publish endpoint makes.</summary>
    protected Task<PublishWorkflowDefinitionResult> PublishAsync(string definitionId) => Publisher.PublishAsync(definitionId);
}

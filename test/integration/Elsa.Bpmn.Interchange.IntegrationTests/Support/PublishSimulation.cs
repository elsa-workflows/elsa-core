using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Xunit;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Support;

/// <summary>
/// Marks the latest version of a definition published, directly on the stored row, without going through
/// <see cref="IWorkflowDefinitionPublisher.PublishAsync(string, CancellationToken)"/> — the one place every test
/// that needs a published-then-edited definition goes through, rather than each duplicating this shortcut.
/// </summary>
/// <remarks>
/// The real publisher's <c>ValidateWorkflowRequestHandler</c> refuses a none-start BPMN process with "Trigger
/// should have a payload" (see elsa-workflows/elsa-core#8078), which the camunda-order-process.bpmn fixture these
/// tests otherwise read unmodified does not satisfy. Only "this row is the published version
/// <see cref="IWorkflowDefinitionPublisher.GetDraftAsync"/> branches on" matters to the tests that use this, so
/// this puts the definition in the same state a successful publish would without exercising that unrelated gate.
/// This helper should become a real <see cref="IWorkflowDefinitionPublisher.PublishAsync(string, CancellationToken)"/>
/// call once #8078 lands.
/// </remarks>
internal static class PublishSimulation
{
    /// <summary>Marks the latest version of <paramref name="definitionId"/> published, directly on the stored row.</summary>
    public static async Task MarkLatestPublishedAsync(IWorkflowDefinitionStore store, string definitionId)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        Assert.NotNull(definition);
        definition!.IsPublished = true;
        await store.SaveAsync(definition);
    }
}

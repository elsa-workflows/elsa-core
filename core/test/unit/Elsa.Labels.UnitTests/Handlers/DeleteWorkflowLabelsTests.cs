using Elsa.Common.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Handlers;
using Elsa.Labels.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Labels.UnitTests.Handlers;

public class DeleteWorkflowLabelsTests
{
    [Fact]
    public async Task VersionDeleted_RemovesOnlyLabelsForDeletedVersion()
    {
        var store = CreateStore();
        await store.SaveManyAsync(
        [
            CreateAssociation("association-1", "definition-1", "version-1"),
            CreateAssociation("association-2", "definition-1", "version-2"),
            CreateAssociation("association-3", "definition-2", "version-3")
        ]);
        var handler = new DeleteWorkflowLabels(store);

        await handler.HandleAsync(
            new WorkflowDefinitionVersionDeleted(new WorkflowDefinition { Id = "version-1", DefinitionId = "definition-1" }),
            CancellationToken.None);

        Assert.Empty(await store.FindByWorkflowDefinitionVersionIdAsync("version-1"));
        Assert.Single(await store.FindByWorkflowDefinitionVersionIdAsync("version-2"));
        Assert.Single(await store.FindByWorkflowDefinitionVersionIdAsync("version-3"));
    }

    [Fact]
    public async Task VersionsDeleted_RemovesOnlyLabelsForSpecifiedVersions()
    {
        var store = CreateStore();
        await store.SaveManyAsync(
        [
            CreateAssociation("association-1", "definition-1", "version-1"),
            CreateAssociation("association-2", "definition-1", "version-2"),
            CreateAssociation("association-3", "definition-1", "version-3")
        ]);
        var handler = new DeleteWorkflowLabels(store);

        await handler.HandleAsync(
            new WorkflowDefinitionVersionsDeleted(["version-1", "version-2"]),
            CancellationToken.None);

        Assert.Empty(await store.FindByWorkflowDefinitionVersionIdAsync("version-1"));
        Assert.Empty(await store.FindByWorkflowDefinitionVersionIdAsync("version-2"));
        Assert.Single(await store.FindByWorkflowDefinitionVersionIdAsync("version-3"));
    }

    [Fact]
    public async Task DefinitionDeleted_StillRemovesLabelsForAllVersionsOfDefinition()
    {
        var store = CreateStore();
        await store.SaveManyAsync(
        [
            CreateAssociation("association-1", "definition-1", "version-1"),
            CreateAssociation("association-2", "definition-1", "version-2"),
            CreateAssociation("association-3", "definition-2", "version-3")
        ]);
        var handler = new DeleteWorkflowLabels(store);

        await handler.HandleAsync(new WorkflowDefinitionDeleted("definition-1"), CancellationToken.None);

        Assert.Empty(await store.FindByWorkflowDefinitionVersionIdAsync("version-1"));
        Assert.Empty(await store.FindByWorkflowDefinitionVersionIdAsync("version-2"));
        Assert.Single(await store.FindByWorkflowDefinitionVersionIdAsync("version-3"));
    }

    private static InMemoryWorkflowDefinitionLabelStore CreateStore() =>
        new(new MemoryStore<WorkflowDefinitionLabel>());

    private static WorkflowDefinitionLabel CreateAssociation(string id, string definitionId, string versionId) =>
        new()
        {
            Id = id,
            WorkflowDefinitionId = definitionId,
            WorkflowDefinitionVersionId = versionId,
            LabelId = "label"
        };
}

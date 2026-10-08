using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;

namespace Elsa.Workflows.Management.UnitTests.Stores;

/// <summary>
/// Memory must reject colliding <c>(DefinitionId, Version, TenantId)</c> keys the same way EF
/// rejects <c>IX_WorkflowDefinition_DefinitionId_Version</c> on Save/SaveMany.
/// </summary>
public class MemoryWorkflowDefinitionStoreUniquenessTests
{
    [Fact(DisplayName = "SaveManyAsync rejects two different Ids that share a version key")]
    public async Task SaveManyAsync_WhenBatchRepeatsVersionKey_ThrowsAndLeavesStoreUnchanged()
    {
        var store = CreateStore();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveManyAsync(
            [
                Definition("def-1", "order"),
                Definition("def-2", "order")
            ]));

        Assert.Contains("already exists", exception.Message);
        Assert.Empty((await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList());
    }

    [Fact(DisplayName = "SaveManyAsync rejects a batch whose version key is already present under another Id")]
    public async Task SaveManyAsync_WhenVersionKeyExistsUnderAnotherId_ThrowsAndLeavesExisting()
    {
        var store = CreateStore();
        await store.SaveAsync(Definition("def-1", "order"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveManyAsync([Definition("def-2", "order")]));

        Assert.Contains("already exists", exception.Message);
        var stored = (await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();
        Assert.Equal("def-1", Assert.Single(stored).Id);
    }

    private static MemoryWorkflowDefinitionStore CreateStore() =>
        new(new MemoryStore<WorkflowDefinition>(), new TestTenantAccessor("tenant-a"));

    private static WorkflowDefinition Definition(string id, string definitionId) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId,
            Name = definitionId,
            TenantId = "tenant-a",
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
        };
}

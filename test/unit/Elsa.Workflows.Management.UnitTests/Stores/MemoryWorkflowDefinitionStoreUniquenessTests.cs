using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using System.Threading.Tasks;

namespace Elsa.Workflows.Management.UnitTests.Stores;

/// <summary>
/// Memory must reject colliding <c>(DefinitionId, Version, TenantId)</c> keys the same way EF
/// rejects <c>IX_WorkflowDefinition_DefinitionId_Version</c> on Save/SaveMany.
/// </summary>
public class MemoryWorkflowDefinitionStoreUniquenessTests
{
    [Test]
    [DisplayName("SaveManyAsync rejects two different Ids that share a version key")]
    public async Task SaveManyAsync_WhenBatchRepeatsVersionKey_ThrowsAndLeavesStoreUnchanged()
    {
        var store = CreateStore();

        var exception = await Assert.That(() => store.SaveManyAsync(
            [
                Definition("def-1", "order"),
                Definition("def-2", "order")
            ])).ThrowsExactly<InvalidOperationException>();

        await Assert.That(exception.Message).Contains("already exists");
        await Assert.That((await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList()).IsEmpty();
    }

    [Test]
    [DisplayName("SaveManyAsync rejects a batch whose version key is already present under another Id")]
    public async Task SaveManyAsync_WhenVersionKeyExistsUnderAnotherId_ThrowsAndLeavesExisting()
    {
        var store = CreateStore();
        await store.SaveAsync(Definition("def-1", "order"));

        var exception = await Assert.That(() => store.SaveManyAsync([Definition("def-2", "order")]))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(exception.Message).Contains("already exists");
        var stored = (await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();
        var single = await Assert.That(stored).HasSingleItem();
        await Assert.That(single.Id).IsEqualTo("def-1");
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

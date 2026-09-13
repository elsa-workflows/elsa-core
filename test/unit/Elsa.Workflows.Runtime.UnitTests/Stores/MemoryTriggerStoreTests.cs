using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

/// <summary>
/// Memory must enforce the same <see cref="StoredTrigger"/> logical uniqueness that EF Core
/// already enforces via the unique index on (WorkflowDefinitionId, Hash, ActivityId, TenantId)
/// and that <c>EFCoreTriggerStore.ReplaceAsync</c> mirrors.
/// </summary>
public class MemoryTriggerStoreTests
{
    [Fact(DisplayName = "ReplaceAsync keeps the first added record when the incoming batch repeats a logical key")]
    public async Task ReplaceAsync_WhenAddedBatchRepeatsLogicalKey_StoresOnlyTheFirst()
    {
        var store = CreateStore();
        var first = Trigger("id-1");
        var duplicate = Trigger("id-2");

        await store.ReplaceAsync([], [first, duplicate]);

        var stored = (await store.FindManyAsync(new TriggerFilter())).ToList();
        var match = Assert.Single(stored);
        Assert.Equal("id-1", match.Id);
    }

    [Fact(DisplayName = "ReplaceAsync skips an added record whose logical key is already present")]
    public async Task ReplaceAsync_WhenLogicalKeyAlreadyPresent_SkipsTheAddedRecord()
    {
        var store = CreateStore();
        var existing = Trigger("existing-id", hash: "hash-1");
        await store.SaveAsync(existing);

        await store.ReplaceAsync([], [Trigger("new-id", hash: "hash-1")]);

        var stored = (await store.FindManyAsync(new TriggerFilter())).ToList();
        var match = Assert.Single(stored);
        Assert.Equal("existing-id", match.Id);
        Assert.Equal("v1", match.WorkflowDefinitionVersionId);
    }

    [Fact(DisplayName = "ReplaceAsync can insert a new Id after the existing logical-key row is removed")]
    public async Task ReplaceAsync_WhenExistingLogicalKeyIsRemoved_InsertsTheReplacement()
    {
        var store = CreateStore();
        var existing = Trigger("existing-id", hash: "hash-1");
        await store.SaveAsync(existing);

        var replacement = Trigger("new-id", hash: "hash-1", workflowDefinitionVersionId: "v2");
        await store.ReplaceAsync([existing], [replacement]);

        var stored = (await store.FindManyAsync(new TriggerFilter())).ToList();
        var match = Assert.Single(stored);
        Assert.Equal("new-id", match.Id);
        Assert.Equal("v2", match.WorkflowDefinitionVersionId);
    }

    [Fact(DisplayName = "ReplaceAsync stamps the current tenant when TenantId is unset")]
    public async Task ReplaceAsync_WhenTenantIdIsUnset_AppliesCurrentTenant()
    {
        var store = CreateStore(new TestTenantAccessor("tenant-a"));
        var trigger = Trigger("id-1");
        trigger.TenantId = null;

        await store.ReplaceAsync([], [trigger]);

        var stored = await store.FindAsync(new TriggerFilter { Id = "id-1" });
        Assert.Equal("tenant-a", stored!.TenantId);
    }

    [Fact(DisplayName = "ReplaceAsync leaves a tenant-agnostic TenantId untouched")]
    public async Task ReplaceAsync_WhenTenantIdIsAgnostic_DoesNotOverwriteTenant()
    {
        var store = CreateStore(new TestTenantAccessor("tenant-a"));
        var trigger = Trigger("id-1");
        trigger.TenantId = Tenant.AgnosticTenantId;

        await store.ReplaceAsync([], [trigger]);

        var stored = await store.FindAsync(new TriggerFilter { Id = "id-1" });
        Assert.Equal(Tenant.AgnosticTenantId, stored!.TenantId);
    }

    [Fact(DisplayName = "ReplaceAsync treats different tenants as distinct logical keys")]
    public async Task ReplaceAsync_WhenTenantIdsDiffer_StoresBothRecords()
    {
        var store = CreateStore();
        var tenantA = Trigger("id-a");
        tenantA.TenantId = "tenant-a";
        var tenantB = Trigger("id-b");
        tenantB.TenantId = "tenant-b";

        await store.ReplaceAsync([], [tenantA, tenantB]);

        var stored = (await store.FindManyAsync(new TriggerFilter())).ToList();
        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, x => x.Id == "id-a");
        Assert.Contains(stored, x => x.Id == "id-b");
    }

    [Fact(DisplayName = "SaveAsync stamps the current tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdIsUnset_AppliesCurrentTenant()
    {
        var store = CreateStore(new TestTenantAccessor("tenant-a"));
        var trigger = Trigger("id-1");
        trigger.TenantId = null;

        await store.SaveAsync(trigger);

        var stored = await store.FindAsync(new TriggerFilter { Id = "id-1" });
        Assert.Equal("tenant-a", stored!.TenantId);
    }

    [Fact(DisplayName = "SaveAsync updates the existing row when the Id matches")]
    public async Task SaveAsync_WhenIdMatches_UpdatesTheExistingRow()
    {
        var store = CreateStore();
        await store.SaveAsync(Trigger("id-1", hash: "hash-1"));

        await store.SaveAsync(Trigger("id-1", hash: "hash-2", workflowDefinitionVersionId: "v2"));

        var stored = await store.FindAsync(new TriggerFilter { Id = "id-1" });
        Assert.Equal("hash-2", stored!.Hash);
        Assert.Equal("v2", stored.WorkflowDefinitionVersionId);
    }

    [Fact(DisplayName = "SaveAsync rejects a different Id that repeats an existing logical key")]
    public async Task SaveAsync_WhenLogicalKeyExistsUnderAnotherId_Throws()
    {
        var store = CreateStore();
        await store.SaveAsync(Trigger("id-1", hash: "hash-1"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveAsync(Trigger("id-2", hash: "hash-1")).AsTask());

        Assert.Contains("already exists", exception.Message);
        var stored = (await store.FindManyAsync(new TriggerFilter())).ToList();
        Assert.Equal("id-1", Assert.Single(stored).Id);
    }

    [Fact(DisplayName = "SaveManyAsync keeps the first record when the batch repeats a logical key")]
    public async Task SaveManyAsync_WhenBatchRepeatsLogicalKey_StoresOnlyTheFirst()
    {
        var store = CreateStore();

        await store.SaveManyAsync([Trigger("id-1"), Trigger("id-2")]);

        var stored = (await store.FindManyAsync(new TriggerFilter())).ToList();
        Assert.Equal("id-1", Assert.Single(stored).Id);
    }

    [Fact(DisplayName = "SaveManyAsync rejects a batch whose logical key is already present under another Id")]
    public async Task SaveManyAsync_WhenLogicalKeyExistsUnderAnotherId_Throws()
    {
        var store = CreateStore();
        await store.SaveAsync(Trigger("id-1", hash: "hash-1"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveManyAsync([Trigger("id-2", hash: "hash-1")]).AsTask());

        Assert.Contains("already exists", exception.Message);
        var stored = (await store.FindManyAsync(new TriggerFilter())).ToList();
        Assert.Equal("id-1", Assert.Single(stored).Id);
    }

    [Fact(DisplayName = "FindAsync returns the single record that matches the filter")]
    public async Task FindAsync_WhenOneLogicalKeyMatches_ReturnsThatRecord()
    {
        var store = CreateStore();
        await store.SaveAsync(Trigger("id-1", hash: "hash-1"));
        await store.SaveAsync(Trigger("id-2", hash: "hash-2"));

        var found = await store.FindAsync(new TriggerFilter { Hash = "hash-1" });

        Assert.Equal("id-1", found!.Id);
    }

    private static MemoryTriggerStore CreateStore(ITenantAccessor? tenantAccessor = null) =>
        new(new MemoryStore<StoredTrigger>(), tenantAccessor ?? TestTenantAccessor.Default);

    private static StoredTrigger Trigger(
        string id,
        string workflowDefinitionId = "workflow-1",
        string workflowDefinitionVersionId = "v1",
        string activityId = "activity-1",
        string hash = "hash-1") =>
        new()
        {
            Id = id,
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowDefinitionVersionId = workflowDefinitionVersionId,
            ActivityId = activityId,
            Hash = hash,
            Name = "Elsa.HttpEndpoint"
        };
}

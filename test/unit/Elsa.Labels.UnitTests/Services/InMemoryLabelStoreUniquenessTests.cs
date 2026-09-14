using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Labels.UnitTests.Services;

/// <summary>
/// Memory must enforce the same per-tenant normalized-name uniqueness that EF Core
/// enforces via <c>IX_Label_TenantId_NormalizedName</c> on <c>(TenantId, NormalizedName)</c>.
/// </summary>
public class InMemoryLabelStoreUniquenessTests
{
    [Test]
    [DisplayName("SaveAsync rejects a different Id that repeats a normalized name in the same tenant")]
    public async Task SaveAsync_WhenNormalizedNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Label("label-1", "Urgent", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(Label("label-2", "urgent", "tenant-a")));

        await Assert.That(exception.Message).Contains("already exists");
        var stored = (await store.ListAsync()).Items.ToList();
        await Assert.That((await Assert.That(stored).HasSingleItem()).Id).IsEqualTo("label-1");
    }

    [Test]
    [DisplayName("SaveAsync allows the same Id to update its own name")]
    public async Task SaveAsync_WhenSameIdUpdatesName_Succeeds()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Label("label-1", "Urgent", "tenant-a"));

        await store.SaveAsync(Label("label-1", "Critical", "tenant-a"));

        var stored = await store.FindByIdAsync("label-1");
        await Assert.That(stored!.Name).IsEqualTo("Critical");
        await Assert.That(stored.NormalizedName).IsEqualTo("critical");
    }

    [Test]
    [DisplayName("SaveAsync allows the same normalized name in different tenants")]
    public async Task SaveAsync_WhenNormalizedNameRepeatsInAnotherTenant_Succeeds()
    {
        var labels = new MemoryStore<Label>();
        var associations = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-b"));

        await tenantA.SaveAsync(Label("label-a", "Urgent", "tenant-a"));
        await tenantB.SaveAsync(Label("label-b", "Urgent", "tenant-b"));

        await Assert.That((await tenantA.FindByIdAsync("label-a"))!.NormalizedName).IsEqualTo("urgent");
        await Assert.That((await tenantB.FindByIdAsync("label-b"))!.NormalizedName).IsEqualTo("urgent");
    }

    [Test]
    [DisplayName("SaveAsync rejects renaming onto a normalized name another Id already owns")]
    public async Task SaveAsync_WhenRenamingOntoAnotherIdsNormalizedName_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Label("label-1", "Urgent", "tenant-a"));
        await store.SaveAsync(Label("label-2", "Later", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(Label("label-2", "Urgent", "tenant-a")));

        await Assert.That(exception.Message).Contains("already exists");
        await Assert.That((await store.FindByIdAsync("label-2"))!.Name).IsEqualTo("Later");
    }

    [Test]
    [DisplayName("SaveAsync leaves the stored name unchanged when a Find result is renamed onto a collision")]
    public async Task SaveAsync_WhenFoundLabelRenamedOntoCollision_LeavesStoredNameUnchanged()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Label("label-1", "Urgent", "tenant-a"));
        await store.SaveAsync(Label("label-2", "Later", "tenant-a"));

        var found = await store.FindByIdAsync("label-2");
        found!.Name = "Urgent";

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.SaveAsync(found));

        await Assert.That(exception.Message).Contains("already exists");
        var stored = await store.FindByIdAsync("label-2");
        await Assert.That(stored!.Name).IsEqualTo("Later");
        await Assert.That(stored.NormalizedName).IsEqualTo("later");
    }

    [Test]
    [DisplayName("SaveAsync treats a stamped ambient tenant as the uniqueness tenant")]
    public async Task SaveAsync_WhenTenantIdUnset_UsesStampedAmbientTenantForUniqueness()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Label("label-1", "Urgent", tenantId: null));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(Label("label-2", "Urgent", tenantId: null)));

        await Assert.That(exception.Message).Contains("already exists");
        await Assert.That((await store.FindByIdAsync("label-1"))!.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync allows the same normalized name for * and a tenant-scoped label")]
    public async Task SaveAsync_WhenAgnosticAndTenantScopedShareNormalizedName_Succeeds()
    {
        var store = CreateStore("tenant-a");

        await store.SaveAsync(Label("label-star", "Urgent", Tenant.AgnosticTenantId));
        await store.SaveAsync(Label("label-a", "Urgent", "tenant-a"));

        await Assert.That(await store.FindByIdAsync("label-star")).IsNotNull();
        await Assert.That(await store.FindByIdAsync("label-a")).IsNotNull();
    }

    [Test]
    [DisplayName("SaveAsync keeps NormalizedName in sync with Name")]
    public async Task SaveAsync_WhenNormalizedNameIsStale_ResyncsFromName()
    {
        var store = CreateStore("tenant-a");
        var label = Label("label-1", "Urgent", "tenant-a");
        label.NormalizedName = "stale";

        await store.SaveAsync(label);

        await Assert.That((await store.FindByIdAsync("label-1"))!.NormalizedName).IsEqualTo("urgent");
    }

    [Test]
    [DisplayName("SaveManyAsync rejects a batch that repeats a normalized name already in the store")]
    public async Task SaveManyAsync_WhenNormalizedNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Label("label-1", "Urgent", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveManyAsync([Label("label-2", "Urgent", "tenant-a")]));

        await Assert.That(exception.Message).Contains("already exists");
        await Assert.That(await store.FindByIdAsync("label-2")).IsNull();
    }

    [Test]
    [DisplayName("SaveManyAsync rejects two different Ids that share a normalized name")]
    public async Task SaveManyAsync_WhenBatchRepeatsNormalizedName_Throws()
    {
        var store = CreateStore("tenant-a");

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveManyAsync([
                Label("label-1", "Urgent", "tenant-a"),
                Label("label-2", "urgent", "tenant-a")
            ]));

        await Assert.That(exception.Message).Contains("already exists");
        await Assert.That((await store.ListAsync()).Items).IsEmpty();
    }

    [Test]
    [DisplayName("SaveManyAsync allows swapping normalized names within one batch")]
    public async Task SaveManyAsync_WhenBatchSwapsNormalizedNames_Succeeds()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Label("label-1", "Urgent", "tenant-a"));
        await store.SaveAsync(Label("label-2", "Later", "tenant-a"));

        await store.SaveManyAsync([
            Label("label-1", "Later", "tenant-a"),
            Label("label-2", "Urgent", "tenant-a")
        ]);

        await Assert.That((await store.FindByIdAsync("label-1"))!.NormalizedName).IsEqualTo("later");
        await Assert.That((await store.FindByIdAsync("label-2"))!.NormalizedName).IsEqualTo("urgent");
    }

    private static InMemoryLabelStore CreateStore(string tenantId) =>
        new(new MemoryStore<Label>(), new MemoryStore<WorkflowDefinitionLabel>(), new TestTenantAccessor(tenantId));

    private static Label Label(string id, string name, string? tenantId) =>
        new()
        {
            Id = id,
            Name = name,
            TenantId = tenantId
        };
}

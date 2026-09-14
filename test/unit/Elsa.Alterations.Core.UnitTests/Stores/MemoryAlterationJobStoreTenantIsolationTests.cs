using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Stores;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Alterations.Core.UnitTests.Stores;

/// <summary>
/// Memory alteration jobs must honor ambient tenant the same way EF does via
/// <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>. Contracts have no TenantAgnostic flag.
/// </summary>
public class MemoryAlterationJobStoreTenantIsolationTests
{
    [Test]
    [DisplayName("FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "job-a");
        await Assert.That(found).Contains(x => x.Id == "job-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "job-b");
    }

    [Test]
    [DisplayName("FindAsync does not return another tenant's row")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-b" });

        await Assert.That(found).IsNull();
    }

    [Test]
    [DisplayName("FindManyIdsAsync hides other tenants")]
    public async Task FindManyIdsAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyIdsAsync(new AlterationJobFilter { PlanId = "plan-1" })).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains("job-a");
        await Assert.That(found).Contains("job-star");
        await Assert.That(found).DoesNotContain("job-b");
    }

    [Test]
    [DisplayName("CountAsync hides other tenants and keeps * visible")]
    public async Task CountAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountAsync(new AlterationJobFilter { PlanId = "plan-1" });

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    [DisplayName("SaveAsync refuses to overwrite another tenant's row by Id")]
    public async Task SaveAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveAsync(Job("shared", "tenant-b")));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveManyAsync refuses to overwrite another tenant's row by Id")]
    public async Task SaveManyAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveManyAsync([Job("shared", "tenant-b")]));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync refuses a forged owner TenantId from another ambient tenant")]
    public async Task SaveAsync_WhenAmbientForgesOwnerTenantId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveAsync(Job("shared", "tenant-a")));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveManyAsync refuses a forged owner TenantId from another ambient tenant")]
    public async Task SaveManyAsync_WhenAmbientForgesOwnerTenantId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveManyAsync([Job("shared", "tenant-a")]));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync refuses to overwrite a tenant-agnostic row by Id")]
    public async Task SaveAsync_WhenAgnosticRowExists_NamedTenantThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var agnostic = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await agnostic.SaveAsync(Job("shared", Tenant.AgnosticTenantId));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveAsync(Job("shared", "tenant-b")));
        var remaining = await agnostic.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveAsync refuses a named source when an agnostic writer updates a * row")]
    public async Task SaveAsync_WhenAgnosticAmbientReceivesNamedSource_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var agnostic = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        await agnostic.SaveAsync(Job("shared", Tenant.AgnosticTenantId));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => agnostic.SaveAsync(Job("shared", "tenant-b")));
        var remaining = await agnostic.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveManyAsync refuses to overwrite a tenant-agnostic row by Id")]
    public async Task SaveManyAsync_WhenAgnosticRowExists_NamedTenantThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var agnostic = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await agnostic.SaveAsync(Job("shared", Tenant.AgnosticTenantId));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveManyAsync([Job("shared", "tenant-b")]));
        var remaining = await agnostic.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveManyAsync refuses a named source when an agnostic writer updates a * row")]
    public async Task SaveManyAsync_WhenAgnosticAmbientReceivesNamedSource_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var agnostic = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        await agnostic.SaveAsync(Job("shared", Tenant.AgnosticTenantId));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => agnostic.SaveManyAsync([Job("shared", "tenant-b")]));
        var remaining = await agnostic.FindAsync(new AlterationJobFilter { Id = "shared" });

        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveAsync still lets an agnostic writer update a * row")]
    public async Task SaveAsync_WhenAmbientIsAgnostic_UpsertsAgnosticRow()
    {
        var store = CreateStore(Tenant.AgnosticTenantId);
        await store.SaveAsync(Job("shared", Tenant.AgnosticTenantId));
        var updated = Job("shared", Tenant.AgnosticTenantId);
        updated.Status = AlterationJobStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "shared" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationJobStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveAsync still upserts a visible same-tenant row")]
    public async Task SaveAsync_WhenSameTenantOwnsId_Upserts()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Job("job-a", "tenant-a"));
        var updated = Job("job-a", "tenant-a");
        updated.Status = AlterationJobStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-a" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationJobStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync preserves the stored TenantId on an accepted update")]
    public async Task SaveAsync_WhenIncomingTenantDiffers_PreservesExistingTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Job("job-a", "tenant-a"));
        var updated = Job("job-a", "tenant-b");
        updated.Status = AlterationJobStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-a" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationJobStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveManyAsync preserves the stored TenantId for repeated accepted updates")]
    public async Task SaveManyAsync_WhenRepeatedIdIncomingTenantsDiffer_PreservesExistingTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Job("job-a", "tenant-a"));
        var first = Job("job-a", "tenant-b");
        var second = Job("job-a", "tenant-c");
        second.Status = AlterationJobStatus.Completed;

        await store.SaveManyAsync([first, second]);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-a" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationJobStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveManyAsync preserves the owner for repeated updates of a new Id")]
    public async Task SaveManyAsync_WhenRepeatedNewIdUsesSameOwner_SucceedsAndPreservesTenantId()
    {
        var store = CreateStore("tenant-a");
        var first = Job("job-new", "tenant-a");
        var second = Job("job-new", "tenant-a");
        second.Status = AlterationJobStatus.Completed;

        await store.SaveManyAsync([first, second]);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-new" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationJobStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveManyAsync rejects a repeated new Id that changes * to a named source")]
    public async Task SaveManyAsync_WhenRepeatedNewIdChangesAgnosticToNamed_ThrowsAndPersistsNothing()
    {
        var store = CreateStore(Tenant.AgnosticTenantId);
        var first = Job("job-new", Tenant.AgnosticTenantId);
        var second = Job("job-new", "tenant-b");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.SaveManyAsync([first, second]));

        await Assert.That(await store.FindAsync(new AlterationJobFilter { Id = "job-new" })).IsNull();
    }

    [Test]
    [DisplayName("SaveManyAsync rejects a repeated new Id that changes its named owner")]
    public async Task SaveManyAsync_WhenRepeatedNewIdChangesNamedOwner_ThrowsAndPersistsNothing()
    {
        var store = CreateStore("tenant-a");
        var first = Job("job-new", "tenant-b");
        var second = Job("job-new", "tenant-c");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.SaveManyAsync([first, second]));

        await Assert.That(await store.FindAsync(new AlterationJobFilter { Id = "job-new" })).IsNull();
    }

    [Test]
    [DisplayName("SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var job = Job("job-new", tenantId: null);

        await store.SaveAsync(job);

        await Assert.That(job.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await store.FindAsync(new AlterationJobFilter { Id = "job-new" }))!.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveManyAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveManyAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var jobs = new[]
        {
            Job("job-1", tenantId: null),
            Job("job-2", tenantId: null)
        };

        await store.SaveManyAsync(jobs);

        foreach (var job in jobs)
        {
            await Assert.That(job.TenantId).IsEqualTo("tenant-a");
        }

        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
        await Assert.That(found.Count).IsEqualTo(2);
        foreach (var job in found)
        {
            await Assert.That(job.TenantId).IsEqualTo("tenant-a");
        }
    }

    [Test]
    [DisplayName("SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = Job("job-star", Tenant.AgnosticTenantId);
        var explicitTenant = Job("job-a", "tenant-a");

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        await Assert.That(agnostic.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
        await Assert.That(explicitTenant.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("FindManyAsync on the default tenant includes null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var backing = new MemoryStore<AlterationJob>();
        backing.Save(Job("job-null", tenantId: null), x => x.Id);
        backing.Save(Job("job-a", "tenant-a"), x => x.Id);
        var store = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.DefaultTenantId));

        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("job-null");
    }

    [Test]
    [DisplayName("FindManyAsync on a named tenant hides null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var backing = new MemoryStore<AlterationJob>();
        backing.Save(Job("job-null", tenantId: null), x => x.Id);
        backing.Save(Job("job-a", "tenant-a"), x => x.Id);
        var store = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));

        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("job-a");
    }

    private static MemoryAlterationJobStore CreateStore(string tenantId) =>
        new(new MemoryStore<AlterationJob>(), new TestTenantAccessor(tenantId));

    private static async Task SeedMixedTenantsAsync(MemoryAlterationJobStore store)
    {
        await store.SaveAsync(Job("job-a", "tenant-a"));
        await store.SaveAsync(Job("job-b", "tenant-b"));
        await store.SaveAsync(Job("job-star", Tenant.AgnosticTenantId));
    }

    private static AlterationJob Job(string id, string? tenantId) =>
        new()
        {
            Id = id,
            PlanId = "plan-1",
            WorkflowInstanceId = "instance-1",
            Status = AlterationJobStatus.Pending,
            TenantId = tenantId
        };
}

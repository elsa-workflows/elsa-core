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
    [Fact(DisplayName = "FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "job-a");
        Assert.Contains(found, x => x.Id == "job-star");
        Assert.DoesNotContain(found, x => x.Id == "job-b");
    }

    [Fact(DisplayName = "FindAsync does not return another tenant's row")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-b" });

        Assert.Null(found);
    }

    [Fact(DisplayName = "FindManyIdsAsync hides other tenants")]
    public async Task FindManyIdsAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyIdsAsync(new AlterationJobFilter { PlanId = "plan-1" })).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains("job-a", found);
        Assert.Contains("job-star", found);
        Assert.DoesNotContain("job-b", found);
    }

    [Fact(DisplayName = "CountAsync hides other tenants and keeps * visible")]
    public async Task CountAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountAsync(new AlterationJobFilter { PlanId = "plan-1" });

        Assert.Equal(2, count);
    }

    [Fact(DisplayName = "SaveAsync refuses to overwrite another tenant's row by Id")]
    public async Task SaveAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tenantB.SaveAsync(Job("shared", "tenant-b")));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        Assert.Contains("shared", ex.Message);
        Assert.NotNull(remaining);
        Assert.Equal("tenant-a", remaining.TenantId);
    }

    [Fact(DisplayName = "SaveManyAsync refuses to overwrite another tenant's row by Id")]
    public async Task SaveManyAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => tenantB.SaveManyAsync([Job("shared", "tenant-b")]));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        Assert.NotNull(remaining);
        Assert.Equal("tenant-a", remaining.TenantId);
    }

    [Fact(DisplayName = "SaveAsync refuses a forged owner TenantId from another ambient tenant")]
    public async Task SaveAsync_WhenAmbientForgesOwnerTenantId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tenantB.SaveAsync(Job("shared", "tenant-a")));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        Assert.Contains("shared", ex.Message);
        Assert.NotNull(remaining);
        Assert.Equal("tenant-a", remaining.TenantId);
    }

    [Fact(DisplayName = "SaveManyAsync refuses a forged owner TenantId from another ambient tenant")]
    public async Task SaveManyAsync_WhenAmbientForgesOwnerTenantId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var tenantA = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Job("shared", "tenant-a"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => tenantB.SaveManyAsync([Job("shared", "tenant-a")]));
        var remaining = await tenantA.FindAsync(new AlterationJobFilter { Id = "shared" });

        Assert.NotNull(remaining);
        Assert.Equal("tenant-a", remaining.TenantId);
    }

    [Fact(DisplayName = "SaveAsync refuses to overwrite a tenant-agnostic row by Id")]
    public async Task SaveAsync_WhenAgnosticRowExists_NamedTenantThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var agnostic = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await agnostic.SaveAsync(Job("shared", Tenant.AgnosticTenantId));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tenantB.SaveAsync(Job("shared", "tenant-b")));
        var remaining = await agnostic.FindAsync(new AlterationJobFilter { Id = "shared" });

        Assert.Contains("shared", ex.Message);
        Assert.NotNull(remaining);
        Assert.Equal(Tenant.AgnosticTenantId, remaining.TenantId);
    }

    [Fact(DisplayName = "SaveManyAsync refuses to overwrite a tenant-agnostic row by Id")]
    public async Task SaveManyAsync_WhenAgnosticRowExists_NamedTenantThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationJob>();
        var agnostic = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        var tenantB = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-b"));
        await agnostic.SaveAsync(Job("shared", Tenant.AgnosticTenantId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => tenantB.SaveManyAsync([Job("shared", "tenant-b")]));
        var remaining = await agnostic.FindAsync(new AlterationJobFilter { Id = "shared" });

        Assert.NotNull(remaining);
        Assert.Equal(Tenant.AgnosticTenantId, remaining.TenantId);
    }

    [Fact(DisplayName = "SaveAsync still lets an agnostic writer update a * row")]
    public async Task SaveAsync_WhenAmbientIsAgnostic_UpsertsAgnosticRow()
    {
        var store = CreateStore(Tenant.AgnosticTenantId);
        await store.SaveAsync(Job("shared", Tenant.AgnosticTenantId));
        var updated = Job("shared", Tenant.AgnosticTenantId);
        updated.Status = AlterationJobStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "shared" });
        Assert.NotNull(found);
        Assert.Equal(AlterationJobStatus.Completed, found.Status);
        Assert.Equal(Tenant.AgnosticTenantId, found.TenantId);
    }

    [Fact(DisplayName = "SaveAsync still upserts a visible same-tenant row")]
    public async Task SaveAsync_WhenSameTenantOwnsId_Upserts()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Job("job-a", "tenant-a"));
        var updated = Job("job-a", "tenant-a");
        updated.Status = AlterationJobStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-a" });
        Assert.NotNull(found);
        Assert.Equal(AlterationJobStatus.Completed, found.Status);
        Assert.Equal("tenant-a", found.TenantId);
    }

    [Fact(DisplayName = "SaveAsync preserves the stored TenantId on an accepted update")]
    public async Task SaveAsync_WhenIncomingTenantDiffers_PreservesExistingTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Job("job-a", "tenant-a"));
        var updated = Job("job-a", "tenant-b");
        updated.Status = AlterationJobStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-a" });
        Assert.NotNull(found);
        Assert.Equal(AlterationJobStatus.Completed, found.Status);
        Assert.Equal("tenant-a", found.TenantId);
    }

    [Fact(DisplayName = "SaveManyAsync preserves the stored TenantId for repeated accepted updates")]
    public async Task SaveManyAsync_WhenRepeatedIdIncomingTenantsDiffer_PreservesExistingTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Job("job-a", "tenant-a"));
        var first = Job("job-a", "tenant-b");
        var second = Job("job-a", "tenant-c");
        second.Status = AlterationJobStatus.Completed;

        await store.SaveManyAsync([first, second]);

        var found = await store.FindAsync(new AlterationJobFilter { Id = "job-a" });
        Assert.NotNull(found);
        Assert.Equal(AlterationJobStatus.Completed, found.Status);
        Assert.Equal("tenant-a", found.TenantId);
    }

    [Fact(DisplayName = "SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var job = Job("job-new", tenantId: null);

        await store.SaveAsync(job);

        Assert.Equal("tenant-a", job.TenantId);
        Assert.Equal("tenant-a", (await store.FindAsync(new AlterationJobFilter { Id = "job-new" }))!.TenantId);
    }

    [Fact(DisplayName = "SaveManyAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveManyAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var jobs = new[]
        {
            Job("job-1", tenantId: null),
            Job("job-2", tenantId: null)
        };

        await store.SaveManyAsync(jobs);

        Assert.All(jobs, job => Assert.Equal("tenant-a", job.TenantId));
        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
        Assert.Equal(2, found.Count);
        Assert.All(found, job => Assert.Equal("tenant-a", job.TenantId));
    }

    [Fact(DisplayName = "SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = Job("job-star", Tenant.AgnosticTenantId);
        var explicitTenant = Job("job-a", "tenant-a");

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        Assert.Equal(Tenant.AgnosticTenantId, agnostic.TenantId);
        Assert.Equal("tenant-a", explicitTenant.TenantId);
    }

    [Fact(DisplayName = "FindManyAsync on the default tenant includes null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var backing = new MemoryStore<AlterationJob>();
        backing.Save(Job("job-null", tenantId: null), x => x.Id);
        backing.Save(Job("job-a", "tenant-a"), x => x.Id);
        var store = new MemoryAlterationJobStore(backing, new TestTenantAccessor(Tenant.DefaultTenantId));

        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();

        Assert.Single(found);
        Assert.Equal("job-null", found[0].Id);
    }

    [Fact(DisplayName = "FindManyAsync on a named tenant hides null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var backing = new MemoryStore<AlterationJob>();
        backing.Save(Job("job-null", tenantId: null), x => x.Id);
        backing.Save(Job("job-a", "tenant-a"), x => x.Id);
        var store = new MemoryAlterationJobStore(backing, new TestTenantAccessor("tenant-a"));

        var found = (await store.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();

        Assert.Single(found);
        Assert.Equal("job-a", found[0].Id);
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

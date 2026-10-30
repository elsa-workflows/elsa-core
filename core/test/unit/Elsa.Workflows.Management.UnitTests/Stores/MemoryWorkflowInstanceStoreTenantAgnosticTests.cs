using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime;

namespace Elsa.Workflows.Management.UnitTests.Stores;

public class MemoryWorkflowInstanceStoreTenantAgnosticTests
{
    private static readonly Dictionary<string, string> TenantById = new()
    {
        ["i-t1"] = "t1",
        ["i-t2"] = "t2",
        ["i-default"] = Tenant.DefaultTenantId,
        ["i-star"] = Tenant.AgnosticTenantId
    };

    [Theory(DisplayName = "Reads and counts honor TenantAgnostic, deletes stay tenant-scoped")]
    [InlineData(true, new[] { "i-default", "i-star", "i-t1", "i-t2" })]
    [InlineData(false, new[] { "i-star", "i-t1" })]
    public async Task ReadsFollowTenantAgnostic_DeleteStaysScoped(bool tenantAgnostic, string[] expectedIds)
    {
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>(), new TestTenantAccessor("t1"));

        foreach (var (id, tenantId) in TenantById)
            await store.SaveAsync(new() { Id = id, TenantId = tenantId });

        var filter = new WorkflowInstanceFilter { TenantAgnostic = tenantAgnostic };
        var summaries = await store.EnumerateSummariesAsync(filter, 1).ToListAsync();

        Assert.Equal(expectedIds, summaries.Select(x => x.Id).Order());
        Assert.All(summaries, x => Assert.Equal(TenantById[x.Id], x.TenantId));
        Assert.Equal(expectedIds.Length, await store.CountAsync(filter));

        await store.DeleteAsync(filter);
        var remaining = await store.FindManyAsync(new() { TenantAgnostic = true });
        var remainingIds = remaining.Select(x => x.Id).ToList();
        Assert.Contains("i-default", remainingIds);
        Assert.Contains("i-t2", remainingIds);
        Assert.DoesNotContain("i-t1", remainingIds);
    }
}

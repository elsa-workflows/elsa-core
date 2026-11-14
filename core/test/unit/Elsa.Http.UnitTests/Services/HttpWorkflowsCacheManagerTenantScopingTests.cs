using Elsa.Caching;
using Elsa.Caching.Options;
using Elsa.Caching.Services;
using Elsa.Common.Multitenancy;
using Elsa.Http.Services;
using Elsa.Workflows;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Http.UnitTests.Services;

/// <summary>
/// Regression tests for https://github.com/elsa-workflows/elsa-core/issues/9217:
/// cache change tokens must be scoped per tenant so that an eviction in one tenant
/// does not invalidate cache entries belonging to another tenant.
/// </summary>
public class HttpWorkflowsCacheManagerTenantScopingTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string WorkflowDefinitionId = "workflow-definition-1";
    private const string BookmarkHash = "http-endpoint:/colliding:get";

    [Fact]
    public async Task EvictWorkflowAsync_InvalidatesOnlyTheCurrentTenant()
    {
        var harness = CreateHarness();

        // Both tenants subscribe to the same workflow definition ID, as CachingHttpWorkflowLookupService does.
        var tenantAEntry = await harness.GetWorkflowEntryAsync(TenantA, WorkflowDefinitionId);
        var tenantBEntry = await harness.GetWorkflowEntryAsync(TenantB, WorkflowDefinitionId);
        Assert.Equal(1, harness.GetWorkflowInvocationCount(TenantA));
        Assert.Equal(1, harness.GetWorkflowInvocationCount(TenantB));

        // Evicting tenant A must evict tenant A's entry...
        harness.TenantAccessor.TenantId = TenantA;
        await harness.CacheManager.EvictWorkflowAsync(WorkflowDefinitionId);

        var tenantAAfterEviction = await harness.GetWorkflowEntryAsync(TenantA, WorkflowDefinitionId);
        var tenantBAfterEviction = await harness.GetWorkflowEntryAsync(TenantB, WorkflowDefinitionId);

        Assert.NotEqual(tenantAEntry, tenantAAfterEviction);
        Assert.Equal(2, harness.GetWorkflowInvocationCount(TenantA));

        // ...but must leave tenant B's entry intact (no factory re-invocation).
        Assert.Equal(tenantBEntry, tenantBAfterEviction);
        Assert.Equal(1, harness.GetWorkflowInvocationCount(TenantB));
    }

    [Fact]
    public async Task EvictTriggerAsync_InvalidatesOnlyTheCurrentTenant()
    {
        var harness = CreateHarness();

        // Both tenants subscribe to the same bookmark hash, as CachingHttpWorkflowLookupService does.
        var tenantAEntry = await harness.GetTriggerEntryAsync(TenantA, BookmarkHash);
        var tenantBEntry = await harness.GetTriggerEntryAsync(TenantB, BookmarkHash);
        Assert.Equal(1, harness.GetTriggerInvocationCount(TenantA));
        Assert.Equal(1, harness.GetTriggerInvocationCount(TenantB));

        // Evicting tenant A must evict tenant A's entry...
        harness.TenantAccessor.TenantId = TenantA;
        await harness.CacheManager.EvictTriggerAsync(BookmarkHash);

        var tenantAAfterEviction = await harness.GetTriggerEntryAsync(TenantA, BookmarkHash);
        var tenantBAfterEviction = await harness.GetTriggerEntryAsync(TenantB, BookmarkHash);

        Assert.NotEqual(tenantAEntry, tenantAAfterEviction);
        Assert.Equal(2, harness.GetTriggerInvocationCount(TenantA));

        // ...but must leave tenant B's entry intact (no factory re-invocation).
        Assert.Equal(tenantBEntry, tenantBAfterEviction);
        Assert.Equal(1, harness.GetTriggerInvocationCount(TenantB));
    }

    [Fact]
    public void ChangeTokenKeys_AreScopedPerTenant()
    {
        var harness = CreateHarness();

        harness.TenantAccessor.TenantId = TenantA;
        var tenantAWorkflowKey = harness.CacheManager.GetWorkflowChangeTokenKey(WorkflowDefinitionId);
        var tenantATriggerKey = harness.CacheManager.GetTriggerChangeTokenKey(BookmarkHash);

        harness.TenantAccessor.TenantId = TenantB;
        var tenantBWorkflowKey = harness.CacheManager.GetWorkflowChangeTokenKey(WorkflowDefinitionId);
        var tenantBTriggerKey = harness.CacheManager.GetTriggerChangeTokenKey(BookmarkHash);

        Assert.NotEqual(tenantAWorkflowKey, tenantBWorkflowKey);
        Assert.NotEqual(tenantATriggerKey, tenantBTriggerKey);
    }

    [Fact]
    public void DefaultTenant_PreservesLegacyChangeTokenKeyFormat()
    {
        var harness = CreateHarness();

        harness.TenantAccessor.TenantId = string.Empty;

        Assert.Equal(
            $"{typeof(HttpWorkflowsCacheManager).FullName}:workflow:{WorkflowDefinitionId}:changeToken",
            harness.CacheManager.GetWorkflowChangeTokenKey(WorkflowDefinitionId));
        Assert.Equal(
            $"{typeof(HttpWorkflowsCacheManager).FullName}:trigger:{BookmarkHash}:changeToken",
            harness.CacheManager.GetTriggerChangeTokenKey(BookmarkHash));
    }

    [Fact]
    public async Task DefaultTenant_EvictionInvalidatesDefaultTenantEntries()
    {
        var harness = CreateHarness();

        var defaultTenantEntry = await harness.GetWorkflowEntryAsync(string.Empty, WorkflowDefinitionId);

        harness.TenantAccessor.TenantId = string.Empty;
        await harness.CacheManager.EvictWorkflowAsync(WorkflowDefinitionId);

        var afterEviction = await harness.GetWorkflowEntryAsync(string.Empty, WorkflowDefinitionId);

        Assert.NotEqual(defaultTenantEntry, afterEviction);
        Assert.Equal(2, harness.GetWorkflowInvocationCount(string.Empty));
    }

    private static CacheHarness CreateHarness() => new();

    private sealed class MutableTenantAccessor : ITenantAccessor
    {
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant => null;
        public IDisposable PushContext(Tenant? tenant) => throw new NotSupportedException();
    }

    /// <summary>
    /// Wires the real Elsa caching stack (memory cache, change token signaler and cache manager) to the
    /// <see cref="HttpWorkflowsCacheManager"/>, and mirrors the subscription pattern used by
    /// <see cref="CachingHttpWorkflowLookupService"/> so tests assert effective invalidation rather than key strings.
    /// </summary>
    private sealed class CacheHarness
    {
        private readonly Dictionary<string, int> _workflowFactoryInvocations = new();
        private readonly Dictionary<string, int> _triggerFactoryInvocations = new();

        public CacheHarness()
        {
            var tenantAccessor = new MutableTenantAccessor();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var changeTokenSignaler = new ChangeTokenSignaler(new ChangeTokenSignalInvoker());
            var cacheManager = new CacheManager(memoryCache, changeTokenSignaler, Microsoft.Extensions.Options.Options.Create(new CachingOptions()));
            Cache = cacheManager;
            CacheManager = new HttpWorkflowsCacheManager(cacheManager, Substitute.For<IHasher>(), tenantAccessor);
            TenantAccessor = tenantAccessor;
        }

        public MutableTenantAccessor TenantAccessor { get; }

        public ICacheManager Cache { get; }

        public IHttpWorkflowsCacheManager CacheManager { get; }

        public int GetWorkflowInvocationCount(string tenantId) => _workflowFactoryInvocations.GetValueOrDefault(tenantId);

        public int GetTriggerInvocationCount(string tenantId) => _triggerFactoryInvocations.GetValueOrDefault(tenantId);

        public async Task<string> GetWorkflowEntryAsync(string tenantId, string workflowDefinitionId)
        {
            TenantAccessor.TenantId = tenantId;
            // Mirror the cache key and change token subscription of CachingHttpWorkflowLookupService.
            return (await Cache.FindOrCreateAsync($"{tenantId}:http-workflow:{workflowDefinitionId}", async entry =>
            {
                entry.AddExpirationToken(Cache.GetToken(CacheManager.GetWorkflowChangeTokenKey(workflowDefinitionId)));
                var invocation = _workflowFactoryInvocations[tenantId] = _workflowFactoryInvocations.GetValueOrDefault(tenantId) + 1;
                return $"workflow:{tenantId}:{workflowDefinitionId}#{invocation}";
            }))!;
        }

        public async Task<string> GetTriggerEntryAsync(string tenantId, string bookmarkHash)
        {
            TenantAccessor.TenantId = tenantId;
            // Mirror the cache key and change token subscription of CachingHttpWorkflowLookupService.
            return (await Cache.FindOrCreateAsync($"{tenantId}:http-workflow:{bookmarkHash}", async entry =>
            {
                entry.AddExpirationToken(Cache.GetToken(CacheManager.GetTriggerChangeTokenKey(bookmarkHash)));
                var invocation = _triggerFactoryInvocations[tenantId] = _triggerFactoryInvocations.GetValueOrDefault(tenantId) + 1;
                return $"trigger:{tenantId}:{bookmarkHash}#{invocation}";
            }))!;
        }
    }
}

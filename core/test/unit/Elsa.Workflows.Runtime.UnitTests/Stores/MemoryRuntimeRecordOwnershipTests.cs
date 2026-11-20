using Elsa.Common.Entities;

using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

public class MemoryRuntimeRecordOwnershipTests
{
    private readonly DefaultTenantAccessor _tenant = new();

    [Fact]
    public async Task ActivityRecordsAreWrittenAndReadWithinTheAmbientTenant()
    {
        var memory = new MemoryStore<ActivityExecutionRecord>();
        var ownerStore = new MemoryActivityExecutionStore(memory, _tenant);
        var otherStore = new MemoryActivityExecutionStore(memory, _tenant);
        var record = new ActivityExecutionRecord { Id = "activity", ActivityName = "owner" };

        using (UseTenant("first"))
        {
            await ownerStore.SaveAsync(record);
            record.TenantId = "second";
        }

        using (UseTenant("second"))
        {
            Assert.Null(await otherStore.FindAsync(new() { Id = "activity" }));
            Assert.Equal(0, await otherStore.CountAsync(new()));
            Assert.Empty(await otherStore.FindManySummariesAsync(new()));
            Assert.Equal(0, await otherStore.DeleteManyAsync(new() { Id = "activity" }));
            await Assert.ThrowsAsync<InvalidOperationException>(() => otherStore.SaveManyAsync(
                [new() { Id = "second-record" }, new() { Id = "activity" }]));
            Assert.Null(await otherStore.FindAsync(new() { Id = "second-record" }));
        }

        using (UseTenant("first"))
            Assert.Equal("owner", (await ownerStore.FindAsync(new() { Id = "activity" }))?.ActivityName);
    }

    [Fact]
    public async Task ExecutionLogPagingAndDeletionAreTenantScoped()
    {
        var store = new MemoryWorkflowExecutionLogStore(new(), _tenant);
        using (UseTenant("first"))
            await store.AddAsync(new() { Id = "first-log" });

        using (UseTenant("second"))
        {
            await store.AddAsync(new() { Id = "second-log" });
            var page = await store.FindManyAsync(new(), PageArgs.All);
            Assert.Equal(1, page.TotalCount);
            Assert.Equal("second-log", Assert.Single(page.Items).Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new() { Id = "first-log" }));
            Assert.Equal(1, await store.DeleteManyAsync(new()));
        }

        using (UseTenant("first"))
            Assert.NotNull(await store.FindAsync(new() { Id = "first-log" }));
    }

    [Fact]
    public async Task DeadLetterReadsAndReplayRespectTenantOwnership()
    {
        var store = new MemoryBookmarkQueueDeadLetterStore(new(), _tenant);
        using (UseTenant("first"))
            await store.AddAsync(new() { Id = "first-letter", OriginalQueueItemId = "queue-item" });

        using (UseTenant("second"))
        {
            Assert.Null(await store.FindAsync(new() { Id = "first-letter" }));
            Assert.Null(await store.TryMarkReplayedAsync("first-letter", "replacement", DateTimeOffset.UnixEpoch));
            var second = await store.AddOrGetExistingAsync(new() { Id = "second-letter", OriginalQueueItemId = "queue-item" });
            Assert.Equal("second-letter", second.Id);
            var page = await store.PageAsync(PageArgs.All, new BookmarkQueueDeadLetterItemOrder<string>(record => record.Id, OrderDirection.Ascending));
            Assert.Equal("second-letter", Assert.Single(page.Items).Id);

            Assert.Equal(2, (await store.FindManyAsync(new BookmarkQueueDeadLetterFilter { TenantAgnostic = true })).Count());
            Assert.Equal(0, await store.DeleteAsync(new() { Id = "first-letter", TenantAgnostic = true }));
        }

        using (UseTenant("first"))
            Assert.True((await store.FindAsync(new() { Id = "first-letter" }))?.CanReplay);
    }

    private IDisposable UseTenant(string id) => _tenant.PushContext(new Tenant { Id = id, Name = id });
}

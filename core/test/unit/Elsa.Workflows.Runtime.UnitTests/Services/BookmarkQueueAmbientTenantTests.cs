using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

/// <summary>
/// Verifies that the ambient tenant is readable on the bookmark-queue signal and worker Start paths.
/// Sequential notification publish (used by <c>DefaultCommitStateHandler.FlushAsync</c>) runs handlers
/// on the same call stack as the publisher, so AsyncLocal tenant flows into <see cref="SignalBookmarkQueueWorker"/>.
/// </summary>
public class BookmarkQueueAmbientTenantTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task SignalBookmarkQueueWorker_CanReadAmbientTenant_WhenSaveNotificationIsHandled()
    {
        var accessor = new DefaultTenantAccessor();
        var signaler = new BookmarkQueueSignaler(accessor);
        var handler = new SignalBookmarkQueueWorker(signaler);
        var tenant = Tenant("tenant-a");
        var awaiter = AwaitTenantAsync(signaler, accessor, "tenant-a");

        using (accessor.PushContext(tenant))
            await handler.HandleAsync(new BookmarkSaved(Bookmark()), CancellationToken.None);

        Assert.Equal("tenant-a", await WaitForSignalAsync(awaiter, "tenant-a"));
    }

    [Fact]
    public void BookmarkQueueWorker_CanReadAmbientTenant_WhenStarted()
    {
        var accessor = new DefaultTenantAccessor();
        var tenant = Tenant("tenant-a");
        var services = new ServiceCollection().AddSingleton<IBookmarkQueueProcessor>(new RecordingBookmarkQueueProcessor()).BuildServiceProvider();
        var worker = new ImmediateBookmarkQueueWorker(
            new BookmarkQueueSignaler(accessor),
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookmarkQueueWorker>.Instance,
            new DefaultTenantScopeFactory(accessor, services.GetRequiredService<IServiceScopeFactory>()),
            accessor);

        using (accessor.PushContext(tenant))
            worker.Start();

        try
        {
            Assert.Equal("tenant-a", worker.CapturedTenantId);
        }
        finally
        {
            worker.Stop();
        }
    }

    [Fact]
    public async Task RecurringTask_ExecuteAsync_SignalsCapturedTenant_WhenAmbientTenantIsMissing()
    {
        var accessor = new DefaultTenantAccessor();
        var signaler = new BookmarkQueueSignaler(accessor);
        var worker = Substitute.For<IBookmarkQueueWorker>();
        var task = new TriggerBookmarkQueueRecurringTask(worker, signaler, accessor);
        var tenant = Tenant("tenant-a");
        var awaiter = AwaitTenantAsync(signaler, accessor, "tenant-a");

        using (accessor.PushContext(tenant))
            await task.StartAsync(CancellationToken.None);

        Assert.Null(accessor.Tenant);
        worker.Received(1).Start();
        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal("tenant-a", await WaitForSignalAsync(awaiter, "tenant-a"));
    }

    private static Task<string> AwaitTenantAsync(IBookmarkQueueSignaler signaler, ITenantAccessor accessor, string tenantId)
    {
        return Task.Run(async () =>
        {
            using (accessor.PushContext(Tenant(tenantId)))
            {
                await signaler.AwaitAsync();
                return accessor.TenantId;
            }
        });
    }

    private static async Task<string> WaitForSignalAsync(Task<string> awaiter, string tenantId)
    {
        try
        {
            return await awaiter.WaitAsync(SignalTimeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Bookmark-queue signal for '{tenantId}' was not received within {SignalTimeout.TotalSeconds:0} seconds.");
        }
    }

    private static Tenant Tenant(string id) => new() { Id = id, Name = id };

    private static StoredBookmark Bookmark() => new()
    {
        Id = "bookmark-1",
        WorkflowInstanceId = "instance-1",
        Hash = "hash"
    };

    private sealed class RecordingBookmarkQueueProcessor : IBookmarkQueueProcessor
    {
        public Task ProcessAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

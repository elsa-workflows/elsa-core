using System.Runtime.CompilerServices;
using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.RealTime;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Sdk;

namespace Elsa.Diagnostics.StructuredLogs.IntegrationTests;

public class StructuredLogsHubTests
{
    private const string ViewPermission = "diagnostics/structured-logs:view";

    [Fact]
    public async Task SubscribeAsync_WhenUserLacksPermission_DeniesAccess()
    {
        var fixture = CreateFixture(tenantId: "tenant-a", permission: "diagnostics/structured-logs:write");

        await Assert.ThrowsAsync<HubException>(() => fixture.Hub.SubscribeAsync(null));
    }

    [Fact]
    public async Task UpdateFilterAsync_WhenUserLacksPermission_DeniesAccess()
    {
        var fixture = CreateFixture(tenantId: "tenant-a", permission: "diagnostics/operations:view");

        await Assert.ThrowsAsync<HubException>(() => fixture.Hub.UpdateFilterAsync(null));
    }

    [Theory]
    [InlineData(ViewPermission)]
    [InlineData("*")]
    [InlineData("*:view")]
    public async Task SubscribeAsync_WhenUserCanRead_StreamsSameTenantEvents(string permission)
    {
        var fixture = CreateFixture(tenantId: "tenant-a", permission, Event("a-1", "tenant-a"));

        await fixture.Hub.SubscribeAsync(null);

        await AssertEventuallyAsync(() => Assert.Equal(["a-1"], fixture.ReceivedIds));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubscribeAsync_WhenFilterIsNull_OnlyStreamsCallerTenantEvents(bool providerReportsDroppedEvents)
    {
        var fixture = CreateFixture("tenant-a", ViewPermission, providerReportsDroppedEvents, Event("b-1", "tenant-b"), Event("a-1", "tenant-a"), Event("b-2", "tenant-b"));

        await fixture.Hub.SubscribeAsync(null);

        await AssertEventuallyAsync(() => Assert.Equal(["a-1"], fixture.ReceivedIds));
        await fixture.ProviderDrained.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(["a-1"], fixture.ReceivedIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tenant-b")]
    public async Task SubscribeAsync_WhenFilterNamesAnotherTenant_DoesNotWidenScope(string? requestedTenantId)
    {
        var fixture = CreateFixture(tenantId: "tenant-a", ViewPermission, Event("b-1", "tenant-b"), Event("a-1", "tenant-a"));

        await fixture.Hub.SubscribeAsync(new StructuredLogFilter { TenantId = requestedTenantId });

        await AssertEventuallyAsync(() => Assert.True(fixture.ProviderDrained.Task.IsCompleted));
        Assert.DoesNotContain("b-1", fixture.ReceivedIds);
    }

    [Fact]
    public async Task UpdateFilterAsync_WhenFilterIsNull_OnlyStreamsCallerTenantEvents()
    {
        var fixture = CreateFixture(tenantId: "tenant-a", ViewPermission, Event("b-1", "tenant-b"), Event("a-1", "tenant-a"));

        await fixture.Hub.UpdateFilterAsync(null);

        await AssertEventuallyAsync(() => Assert.Equal(["a-1"], fixture.ReceivedIds));
        await fixture.ProviderDrained.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(["a-1"], fixture.ReceivedIds);
    }

    [Fact]
    public async Task SubscribeAsync_WhenCallerIsDefaultTenant_DoesNotStreamOtherTenantEvents()
    {
        var fixture = CreateFixture(tenantId: "", ViewPermission, Event("b-1", "tenant-b"), Event("host-1", null), Event("agnostic-1", "*"));

        await fixture.Hub.SubscribeAsync(null);

        await AssertEventuallyAsync(() => Assert.Equal(["host-1", "agnostic-1"], fixture.ReceivedIds));
        await fixture.ProviderDrained.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(["host-1", "agnostic-1"], fixture.ReceivedIds);
    }

    [Fact]
    public async Task SubscribeAsync_WhenTenantAccessorIsNotRegistered_StreamsAllEvents()
    {
        var fixture = CreateFixture(tenantId: null, ViewPermission, Event("a-1", "tenant-a"), Event("b-1", "tenant-b"));

        await fixture.Hub.SubscribeAsync(null);

        await AssertEventuallyAsync(() => Assert.Equal(["a-1", "b-1"], fixture.ReceivedIds));
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserCanRead_JoinsSourceChangesGroup()
    {
        var fixture = CreateFixture(tenantId: "tenant-a", ViewPermission);

        await fixture.Hub.OnConnectedAsync();

        Assert.Equal([StructuredLogsHub.SourceChangesGroup], fixture.JoinedGroups);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserLacksPermission_DoesNotJoinSourceChangesGroup()
    {
        var fixture = CreateFixture(tenantId: "tenant-a", permission: "diagnostics/structured-logs:write");

        await fixture.Hub.OnConnectedAsync();

        Assert.Empty(fixture.JoinedGroups);
    }

    [Fact]
    public async Task SourceChanged_NotifiesSourceChangesGroupOnly()
    {
        var fixture = CreateFixture(tenantId: "tenant-a", ViewPermission);

        fixture.SourceRegistry.Raise(new StructuredLogSource { Id = "source-1", DisplayName = "Source 1" });

        await AssertEventuallyAsync(() =>
        {
            Assert.Equal([StructuredLogsHub.SourceChangesGroup], fixture.NotifiedGroups);
            Assert.Equal(["source-1"], fixture.ReceivedSourceIds);
        });
    }

    private static StructuredLogEvent Event(string id, string? tenantId) => new() { Id = id, TenantId = tenantId, Category = "Test", SourceId = "source-1" };

    private static Fixture CreateFixture(string? tenantId, string permission, params StructuredLogEvent[] events) => CreateFixture(tenantId, permission, false, events);

    private static Fixture CreateFixture(string? tenantId, string permission, bool providerReportsDroppedEvents, params StructuredLogEvent[] events) => new(tenantId, permission, providerReportsDroppedEvents, events);

    private static async Task AssertEventuallyAsync(Action assertion)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(3);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                assertion();
                return;
            }
            catch (XunitException e)
            {
                lastException = e;
                await Task.Delay(25);
            }
        }

        if (lastException != null)
            throw lastException;
    }

    private sealed class Fixture : IStructuredLogsClient
    {
        private readonly List<string> _receivedIds = [];
        private readonly List<string> _receivedSourceIds = [];
        private readonly List<string> _joinedGroups = [];
        private readonly List<string> _notifiedGroups = [];

        public Fixture(string? tenantId, string permission, bool providerReportsDroppedEvents, StructuredLogEvent[] events)
        {
            Provider = providerReportsDroppedEvents ? new StreamingProvider(events, ProviderDrained) : new Provider(events, ProviderDrained);
            SourceRegistry = new TestSourceRegistry();
            var hubContext = new TestHubContext(this, _notifiedGroups);
            var manager = new StructuredLogSubscriptionManager(Provider, SourceRegistry, hubContext, NullLogger<StructuredLogSubscriptionManager>.Instance);

            Hub = new StructuredLogsHub(manager, tenantId == null ? null : new TestTenantAccessor(tenantId))
            {
                Context = new TestHubCallerContext(CreateUser(permission)),
                Clients = new TestHubCallerClients(this),
                Groups = new TestGroupManager(_joinedGroups)
            };
        }

        public StructuredLogsHub Hub { get; }
        public IStructuredLogProvider Provider { get; }
        public TestSourceRegistry SourceRegistry { get; }
        public TaskCompletionSource ProviderDrained { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IReadOnlyList<string> ReceivedIds => Snapshot(_receivedIds);
        public IReadOnlyList<string> ReceivedSourceIds => Snapshot(_receivedSourceIds);
        public IReadOnlyList<string> JoinedGroups => Snapshot(_joinedGroups);
        public IReadOnlyList<string> NotifiedGroups => Snapshot(_notifiedGroups);

        private static string[] Snapshot(List<string> list)
        {
            lock (list)
                return list.ToArray();
        }

        public Task ReceiveLogEventAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
        {
            lock (_receivedIds)
                _receivedIds.Add(logEvent.Id);

            return Task.CompletedTask;
        }

        public Task ReceiveDroppedEventsAsync(StructuredLogDroppedEventSummary summary, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ReceiveSourceChangedAsync(StructuredLogSource source, CancellationToken cancellationToken = default)
        {
            lock (_receivedSourceIds)
                _receivedSourceIds.Add(source.Id);

            return Task.CompletedTask;
        }
    }

    private static ClaimsPrincipal CreateUser(string permission) => new(new ClaimsIdentity([new Claim(Elsa.PermissionNames.ClaimType, permission)], "Test"));

    private class Provider(StructuredLogEvent[] events, TaskCompletionSource drained) : IStructuredLogProvider
    {
        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var logEvent in events.Where(x => StructuredLogFilterEvaluator.Matches(x, filter)))
            {
                yield return logEvent;
                await Task.Yield();
            }

            drained.TrySetResult();
        }

        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StreamingProvider(StructuredLogEvent[] events, TaskCompletionSource drained) : Provider(events, drained), IStructuredLogStreamProvider
    {
        public async IAsyncEnumerable<StructuredLogStreamItem> SubscribeWithDroppedEventsAsync(StructuredLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var logEvent in SubscribeAsync(filter, cancellationToken))
                yield return StructuredLogStreamItem.FromLogEvent(logEvent);
        }
    }

    private sealed class TestTenantAccessor(string tenantId) : ITenantAccessor
    {
        public string TenantId { get; } = tenantId;
        public Tenant? Tenant => null;
        public IDisposable PushContext(Tenant? tenant) => throw new NotSupportedException();
    }

    private sealed class TestSourceRegistry : IStructuredLogSourceRegistry
    {
        public event Action<StructuredLogSource>? SourceChanged;
        public StructuredLogSource Current => throw new NotSupportedException();
        public void MarkSeen(string sourceId, DateTimeOffset timestamp) => throw new NotSupportedException();
        public IReadOnlyCollection<StructuredLogSource> List() => throw new NotSupportedException();
        public void Raise(StructuredLogSource source) => SourceChanged?.Invoke(source);
    }

    private sealed class TestHubContext(IStructuredLogsClient client, List<string> notifiedGroups) : IHubContext<StructuredLogsHub, IStructuredLogsClient>
    {
        public IHubClients<IStructuredLogsClient> Clients { get; } = new TestHubClients(client, notifiedGroups);
        public IGroupManager Groups { get; } = new TestGroupManager([]);
    }

    private sealed class TestHubClients(IStructuredLogsClient client, List<string> notifiedGroups) : IHubClients<IStructuredLogsClient>
    {
        // Anything that is not addressed to a single connection or an explicit group would reach unauthorized clients.
        public IStructuredLogsClient All => throw new NotSupportedException();
        public IStructuredLogsClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IStructuredLogsClient Client(string connectionId) => client;
        public IStructuredLogsClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

        public IStructuredLogsClient Group(string groupName)
        {
            lock (notifiedGroups)
                notifiedGroups.Add(groupName);

            return client;
        }

        public IStructuredLogsClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IStructuredLogsClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IStructuredLogsClient User(string userId) => throw new NotSupportedException();
        public IStructuredLogsClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class TestHubCallerClients(IStructuredLogsClient caller) : IHubCallerClients<IStructuredLogsClient>
    {
        public IStructuredLogsClient Caller { get; } = caller;
        public IStructuredLogsClient Others => throw new NotSupportedException();
        public IStructuredLogsClient All => throw new NotSupportedException();
        public IStructuredLogsClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IStructuredLogsClient Client(string connectionId) => throw new NotSupportedException();
        public IStructuredLogsClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IStructuredLogsClient Group(string groupName) => throw new NotSupportedException();
        public IStructuredLogsClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IStructuredLogsClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IStructuredLogsClient OthersInGroup(string groupName) => throw new NotSupportedException();
        public IStructuredLogsClient User(string userId) => throw new NotSupportedException();
        public IStructuredLogsClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class TestGroupManager(List<string> joinedGroups) : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            lock (joinedGroups)
                joinedGroups.Add(groupName);

            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestHubCallerContext(ClaimsPrincipal user) : HubCallerContext
    {
        public override string ConnectionId { get; } = "connection-1";
        public override string? UserIdentifier { get; } = "user-1";
        public override ClaimsPrincipal? User { get; } = user;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted { get; } = CancellationToken.None;

        public override void Abort()
        {
        }
    }
}

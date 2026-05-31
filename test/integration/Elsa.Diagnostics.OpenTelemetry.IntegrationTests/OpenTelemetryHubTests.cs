using System.Security.Claims;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using Elsa.Diagnostics.OpenTelemetry.Providers.InMemory;
using Elsa.Diagnostics.OpenTelemetry.RealTime;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Sdk;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.OpenTelemetry.IntegrationTests;

public class OpenTelemetryHubTests
{
    private readonly DateTimeOffset _now = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubscribeAsync_WhenUserLacksPermission_DeniesAccess()
    {
        var hub = CreateHub(new TestLiveFeed(), "write:diagnostics:opentelemetry");

        await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(new()));
    }

    [Theory]
    [InlineData(OpenTelemetryPermissions.Read)]
    [InlineData(PermissionNames.All)]
    [InlineData("read:*")]
    public async Task SubscribeAsync_WhenUserCanRead_ForwardsItemsToCaller(string permission)
    {
        var liveFeed = new TestLiveFeed(new OpenTelemetryStreamItem { Trace = Trace("trace-1") });
        var caller = new CapturingOpenTelemetryClient();
        var hub = CreateHub(liveFeed, permission, caller);

        await hub.SubscribeAsync(new OpenTelemetryTraceFilter { TraceId = "trace-1" });

        await AssertEventuallyAsync(() =>
        {
            Assert.Equal("trace-1", Assert.Single(caller.Items).Trace?.TraceId);
            Assert.Equal("trace-1", liveFeed.Filter?.TraceId);
        });
    }

    [Fact]
    public async Task SubscribeAsync_WhenFilterTimeRangeIsInvalid_RejectsFilter()
    {
        var hub = CreateHub(new TestLiveFeed(), OpenTelemetryPermissions.Read);

        await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(new OpenTelemetryTraceFilter
        {
            From = _now.AddMinutes(1),
            To = _now
        }));
    }

    [Fact]
    public async Task LiveFeed_WhenTraceFilterIsSet_OnlyPublishesMatchingTraces()
    {
        var liveFeed = new InMemoryOpenTelemetryLiveFeed(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions()));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var enumerator = liveFeed.SubscribeAsync(new OpenTelemetryTraceFilter { TraceId = "trace-keep" }, timeout.Token).GetAsyncEnumerator(timeout.Token);
        var next = enumerator.MoveNextAsync().AsTask();

        await liveFeed.PublishAsync(new OpenTelemetryBatch([], [Trace("trace-skip"), Trace("trace-keep")], [], [], [], []), timeout.Token);

        Assert.True(await next);
        Assert.Equal("trace-keep", enumerator.Current.Trace?.TraceId);
    }

    [Fact]
    public async Task LiveFeed_WhenServiceNameFilterIsSet_OnlyPublishesMatchingResourcesAndTraces()
    {
        var liveFeed = new InMemoryOpenTelemetryLiveFeed(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions()));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var enumerator = liveFeed.SubscribeAsync(new OpenTelemetryTraceFilter { ServiceName = "api" }, timeout.Token).GetAsyncEnumerator(timeout.Token);
        var next = enumerator.MoveNextAsync().AsTask();

        await liveFeed.PublishAsync(new OpenTelemetryBatch(
            [Resource("resource-skip", "worker")],
            [Trace("trace-skip", ["resource-skip"])],
            [], [], [], []), timeout.Token);

        var completed = await Task.WhenAny(next, Task.Delay(TimeSpan.FromMilliseconds(150), timeout.Token));
        Assert.NotSame(next, completed);

        await liveFeed.PublishAsync(new OpenTelemetryBatch(
            [Resource("resource-keep", "api")],
            [Trace("trace-keep", ["resource-keep"])],
            [], [], [], []), timeout.Token);

        Assert.True(await next);
        Assert.Equal("resource-keep", enumerator.Current.Resource?.Id);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("trace-keep", enumerator.Current.Trace?.TraceId);
    }

    [Fact]
    public async Task LiveFeed_WhenResourceFilterIsSet_FiltersLogsAndMetricPoints()
    {
        var liveFeed = new InMemoryOpenTelemetryLiveFeed(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions()));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var enumerator = liveFeed.SubscribeAsync(new OpenTelemetryTraceFilter { ResourceId = "resource-keep" }, timeout.Token).GetAsyncEnumerator(timeout.Token);
        var first = enumerator.MoveNextAsync().AsTask();

        await liveFeed.PublishAsync(new OpenTelemetryBatch(
            [],
            [],
            [],
            [],
            [MetricPoint("point-skip", "resource-skip"), MetricPoint("point-keep", "resource-keep")],
            [Log("log-skip", "resource-skip"), Log("log-keep", "resource-keep")]), timeout.Token);

        Assert.True(await first);
        Assert.Equal("log-keep", enumerator.Current.Log?.Id);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("point-keep", enumerator.Current.MetricPoint?.Id);
    }

    [Fact]
    public async Task LiveFeed_WhenSubscriberQueueOverflows_PublishesDroppedItemSummary()
    {
        var liveFeed = new InMemoryOpenTelemetryLiveFeed(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions { SubscriberChannelCapacity = 1 }));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var enumerator = liveFeed.SubscribeAsync(new OpenTelemetryTraceFilter(), timeout.Token).GetAsyncEnumerator(timeout.Token);
        var first = enumerator.MoveNextAsync().AsTask();

        await liveFeed.PublishAsync(new OpenTelemetryBatch([], [Trace("trace-1"), Trace("trace-2"), Trace("trace-3")], [], [], [], []), timeout.Token);

        Assert.True(await first);

        OpenTelemetryStreamItem? summary = enumerator.Current.DroppedItems != null ? enumerator.Current : null;
        for (var i = 0; summary == null && i < 5 && await enumerator.MoveNextAsync(); i++)
        {
            if (enumerator.Current.DroppedItems != null)
                summary = enumerator.Current;
        }

        Assert.NotNull(summary);
        var nonNullSummary = summary!;
        Assert.Equal(OpenTelemetrySignalType.Trace, nonNullSummary.DroppedItems!.SignalType);
        Assert.Equal("SubscriberQueueFull", nonNullSummary.DroppedItems.Reason);
        Assert.True(nonNullSummary.DroppedItems.Count > 0);
    }

    private OpenTelemetryHub CreateHub(IOpenTelemetryLiveFeed liveFeed, string permission, IOpenTelemetryClient? caller = null)
    {
        caller ??= new CapturingOpenTelemetryClient();
        var hubContext = new TestHubContext(caller);
        var subscriptionManager = new OpenTelemetrySubscriptionManager(liveFeed, hubContext, NullLogger<OpenTelemetrySubscriptionManager>.Instance);

        return new OpenTelemetryHub(subscriptionManager)
        {
            Context = new TestHubCallerContext(CreateUser(permission)),
            Clients = new TestHubCallerClients(caller)
        };
    }

    private ClaimsPrincipal CreateUser(string permission)
    {
        var permissionClaimType = (string)typeof(SecurityOptions)
            .GetProperty(nameof(SecurityOptions.PermissionsClaimType))!
            .GetValue(new Config().Security)!;
        var identity = new ClaimsIdentity([new Claim(permissionClaimType, permission)], "Test");

        return new ClaimsPrincipal(identity);
    }

    private TelemetryResource Resource(string id, string serviceName)
    {
        return new(id, serviceName, null, null, new Dictionary<string, string?>(), _now, TelemetryResourceStatus.Active);
    }

    private TelemetryTrace Trace(string traceId, IReadOnlyCollection<string>? resourceIds = null, IReadOnlyCollection<string>? workflowInstanceIds = null)
    {
        return new(traceId, $"{traceId}-root", traceId, _now, _now.AddMilliseconds(10), TimeSpan.FromMilliseconds(10), SpanStatus.Ok, resourceIds ?? [], workflowInstanceIds ?? [], 1);
    }

    private MetricPoint MetricPoint(string id, string resourceId)
    {
        return new(id, $"{id}-instrument", $"{id}-instrument", resourceId, _now, 1, null, null, new Dictionary<string, string?>(), null, null);
    }

    private OtlpLogRecord Log(string id, string resourceId)
    {
        return new(id, resourceId, _now, "Information", 9, id, null, null, new Dictionary<string, string?>());
    }

    private class TestLiveFeed(params OpenTelemetryStreamItem[] items) : IOpenTelemetryLiveFeed
    {
        public OpenTelemetryTraceFilter? Filter { get; private set; }

        public ValueTask PublishAsync(OpenTelemetryBatch batch, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public async IAsyncEnumerable<OpenTelemetryStreamItem> SubscribeAsync(OpenTelemetryTraceFilter filter, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Filter = filter;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }
    }

    private class CapturingOpenTelemetryClient : IOpenTelemetryClient
    {
        public List<OpenTelemetryStreamItem> Items { get; } = [];

        public Task ReceiveAsync(OpenTelemetryStreamItem item)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }
    }

    private class TestHubCallerClients(IOpenTelemetryClient caller) : IHubCallerClients<IOpenTelemetryClient>
    {
        public IOpenTelemetryClient Caller { get; } = caller;
        public IOpenTelemetryClient Others => throw new NotSupportedException();
        public IOpenTelemetryClient All => throw new NotSupportedException();
        public IOpenTelemetryClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IOpenTelemetryClient Client(string connectionId) => throw new NotSupportedException();
        public IOpenTelemetryClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IOpenTelemetryClient Group(string groupName) => throw new NotSupportedException();
        public IOpenTelemetryClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IOpenTelemetryClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IOpenTelemetryClient OthersInGroup(string groupName) => throw new NotSupportedException();
        public IOpenTelemetryClient User(string userId) => throw new NotSupportedException();
        public IOpenTelemetryClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private class TestHubContext(IOpenTelemetryClient caller) : IHubContext<OpenTelemetryHub, IOpenTelemetryClient>
    {
        public IHubClients<IOpenTelemetryClient> Clients { get; } = new TestHubClients(caller);

        public IGroupManager Groups { get; } = new TestGroupManager();
    }

    private class TestHubClients(IOpenTelemetryClient caller) : IHubClients<IOpenTelemetryClient>
    {
        public IOpenTelemetryClient All => throw new NotSupportedException();
        public IOpenTelemetryClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IOpenTelemetryClient Client(string connectionId) => caller;
        public IOpenTelemetryClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IOpenTelemetryClient Group(string groupName) => throw new NotSupportedException();
        public IOpenTelemetryClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IOpenTelemetryClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IOpenTelemetryClient User(string userId) => throw new NotSupportedException();
        public IOpenTelemetryClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private class TestGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

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

    private class TestHubCallerContext(ClaimsPrincipal user) : HubCallerContext
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

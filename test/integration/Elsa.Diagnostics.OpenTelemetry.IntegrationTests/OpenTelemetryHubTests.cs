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

        Assert.Equal("trace-1", Assert.Single(caller.Items).Trace?.TraceId);
        Assert.Equal("trace-1", liveFeed.Filter?.TraceId);
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
    public async Task LiveFeed_WhenSubscriberQueueOverflows_PublishesDroppedItemSummary()
    {
        var liveFeed = new InMemoryOpenTelemetryLiveFeed(OptionsFactory.Create(new OpenTelemetryDiagnosticsOptions { SubscriberChannelCapacity = 1 }));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var enumerator = liveFeed.SubscribeAsync(new OpenTelemetryTraceFilter(), timeout.Token).GetAsyncEnumerator(timeout.Token);
        var first = enumerator.MoveNextAsync().AsTask();

        await liveFeed.PublishAsync(new OpenTelemetryBatch([], [Trace("trace-1"), Trace("trace-2"), Trace("trace-3")], [], [], [], []), timeout.Token);

        Assert.True(await first);

        var observed = new[] { enumerator.Current };
        if (enumerator.Current.DroppedItems == null && await enumerator.MoveNextAsync())
            observed = [.. observed, enumerator.Current];

        var summary = Assert.Single(observed, x => x.DroppedItems != null);
        Assert.Equal(OpenTelemetrySignalType.Trace, summary.DroppedItems!.SignalType);
        Assert.Equal("SubscriberQueueFull", summary.DroppedItems.Reason);
        Assert.True(summary.DroppedItems.Count > 0);
    }

    private OpenTelemetryHub CreateHub(IOpenTelemetryLiveFeed liveFeed, string permission, IOpenTelemetryClient? caller = null)
    {
        return new OpenTelemetryHub(liveFeed)
        {
            Context = new TestHubCallerContext(CreateUser(permission)),
            Clients = new TestHubCallerClients(caller ?? new CapturingOpenTelemetryClient())
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

    private TelemetryTrace Trace(string traceId)
    {
        return new(traceId, $"{traceId}-root", traceId, _now, _now.AddMilliseconds(10), TimeSpan.FromMilliseconds(10), SpanStatus.Ok, [], [], 1);
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

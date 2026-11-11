using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Services;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.Filtering;

public class StructuredLogTenantScopeTests
{
    [Theory]
    [InlineData("tenant-a", "tenant-a", true)]
    [InlineData("tenant-b", "tenant-a", false)]
    [InlineData(null, "tenant-a", false)]
    [InlineData("*", "tenant-a", true)]
    [InlineData(null, "", true)]
    [InlineData("", "", true)]
    [InlineData("tenant-b", "", false)]
    [InlineData("tenant-b", null, true)]
    public void IsVisibleToTenant_ReturnsWhetherCallerMaySeeEvent(string? eventTenantId, string? callerTenantId, bool expected)
    {
        Assert.Equal(expected, StructuredLogFilterEvaluator.IsVisibleToTenant(Event("e", eventTenantId), callerTenantId));
    }

    [Fact]
    public async Task GetRecentForTenantAsync_WhenCallerNamesAnotherTenant_ScopesQueryToCallerTenant()
    {
        var provider = new RecentProvider(Event("a-1", "tenant-a"), Event("b-1", "tenant-b"));

        var result = await provider.GetRecentForTenantAsync(new StructuredLogFilter { TenantId = "tenant-b" }, "tenant-a");

        Assert.Equal("tenant-a", provider.Filter!.TenantId);
        Assert.Equal(["a-1"], result.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetRecentForTenantAsync_WhenFilterHasNoTenant_ReturnsOnlyCallerTenantEvents()
    {
        var provider = new RecentProvider(Event("b-1", "tenant-b"), Event("a-1", "tenant-a"), Event("host-1", null));

        var result = await provider.GetRecentForTenantAsync(new StructuredLogFilter(), "tenant-a");

        Assert.Equal(["a-1"], result.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetRecentForTenantAsync_WhenCallerIsDefaultTenant_ReturnsOnlyUntaggedEvents()
    {
        var provider = new RecentProvider(Event("b-1", "tenant-b"), Event("host-1", null));

        var result = await provider.GetRecentForTenantAsync(new StructuredLogFilter { TenantId = "tenant-b" }, "");

        Assert.Empty(result.Items);

        result = await provider.GetRecentForTenantAsync(new StructuredLogFilter(), "");

        Assert.Equal(["host-1"], result.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetRecentForTenantAsync_WhenTenancyIsNotInUse_DoesNotClamp()
    {
        var provider = new RecentProvider(Event("b-1", "tenant-b"), Event("a-1", "tenant-a"));

        var result = await provider.GetRecentForTenantAsync(new StructuredLogFilter(), null);

        Assert.Equal(["b-1", "a-1"], result.Items.Select(x => x.Id));
    }

    private static StructuredLogEvent Event(string id, string? tenantId) => new() { Id = id, TenantId = tenantId, Category = "Test", SourceId = "source-1" };

    private sealed class RecentProvider(params StructuredLogEvent[] events) : IStructuredLogProvider
    {
        public StructuredLogFilter? Filter { get; private set; }

        public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return ValueTask.FromResult(new RecentStructuredLogsResult(events.Where(x => StructuredLogFilterEvaluator.Matches(x, filter)).ToList(), 0));
        }

        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

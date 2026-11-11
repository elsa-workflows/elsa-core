using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Features;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Testing.Shared.Authorization;
using Elsa.UnitTests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.IntegrationTests;

[Collection(nameof(FastEndpointsCollection))]
public class StructuredLogsEndpointTests : IAsyncLifetime
{
    private const string ViewPermission = "diagnostics/structured-logs:view";
    private const string Namespace = "Elsa.Diagnostics.StructuredLogs.Endpoints";
    private readonly RecentProvider _provider = new(Event("b-1", "tenant-b"), Event("agnostic-1", "*"), Event("a-1", "tenant-a"), Event("host-1", null));
    private TestTenantAccessor _tenantAccessor = new("tenant-a");
    private AuthorizationTestHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = await AuthorizationTestHost.StartAsync<StructuredLogsFeature>(
            type => type.Namespace?.StartsWith(Namespace, StringComparison.Ordinal) == true,
            services =>
            {
                services.AddSingleton<IStructuredLogProvider>(_provider);
                services.AddSingleton<ITenantAccessor>(_ => _tenantAccessor);
            });
    }

    public async Task DisposeAsync()
    {
        FastEndpointsResolver.Reset();
        await _host.DisposeAsync();
    }

    [Fact]
    public void Module_HasExactlyTheEndpointsThePermissionTestsCover()
    {
        var endpoints = EndpointCoverage.FindEndpoints(typeof(StructuredLogsFeature).Assembly).Select(x => x.FullName).Order().ToArray();

        Assert.Equal(
            new[]
            {
                $"{Namespace}.StructuredLogs.Recent.Endpoint",
                $"{Namespace}.StructuredLogs.Sources.Endpoint",
                $"{Namespace}.StructuredLogs.Storage.Endpoint"
            }, endpoints!);
    }

    [Theory]
    [InlineData("/diagnostics/structured-logs/recent")]
    [InlineData("/diagnostics/structured-logs/sources")]
    [InlineData("/diagnostics/structured-logs/storage")]
    public async Task Endpoint_RequiresStructuredLogsViewPermission(string path)
    {
        using var anonymous = await _host.SendAsync(HttpMethod.Get, path);
        using var withoutPermission = await _host.SendAsync(HttpMethod.Get, path, "diagnostics/structured-logs:write");
        using var otherDiagnostics = await _host.SendAsync(HttpMethod.Get, path, "diagnostics/opentelemetry:view");
        using var withPermission = await _host.SendAsync(HttpMethod.Get, path, ViewPermission);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, withoutPermission.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherDiagnostics.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withPermission.StatusCode);
    }

    [Theory]
    [InlineData("", new[] { "a-1" })]
    [InlineData("?TenantId=tenant-b", new[] { "a-1" })]
    [InlineData("?TenantId=*", new[] { "a-1" })]
    public async Task Recent_ReturnsOnlyCallerTenantEvents(string query, string[] expectedIds)
    {
        using var response = await _host.SendAsync(HttpMethod.Get, $"/diagnostics/structured-logs/recent{query}", ViewPermission);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedIds, await ReadIdsAsync(response));
        Assert.Equal("tenant-a", _provider.Filter!.TenantId);
    }

    [Fact]
    public async Task Recent_WhenCallerIsDefaultTenant_ReturnsOnlyUntaggedAndAgnosticEvents()
    {
        _tenantAccessor = new("");

        using var response = await _host.SendAsync(HttpMethod.Get, "/diagnostics/structured-logs/recent?TenantId=tenant-b", ViewPermission);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await ReadIdsAsync(response));

        using var unfiltered = await _host.SendAsync(HttpMethod.Get, "/diagnostics/structured-logs/recent", ViewPermission);

        Assert.Equal(new[] { "agnostic-1", "host-1" }, await ReadIdsAsync(unfiltered));
    }

    private static async Task<string[]> ReadIdsAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetString()!).ToArray();
    }

    private static StructuredLogEvent Event(string id, string? tenantId) => new() { Id = id, TenantId = tenantId, Category = "Test", SourceId = "source-1" };

    private sealed class RecentProvider(params StructuredLogEvent[] events) : IStructuredLogProvider
    {
        public StructuredLogFilter? Filter { get; private set; }

        public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return ValueTask.FromResult(new RecentStructuredLogsResult(events.Where(x => Services.StructuredLogFilterEvaluator.Matches(x, filter)).ToList(), 0));
        }

        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyCollection<StructuredLogSource>>([]);
    }

    private sealed class TestTenantAccessor(string tenantId) : ITenantAccessor
    {
        public string TenantId { get; } = tenantId;
        public Tenant? Tenant => null;
        public IDisposable PushContext(Tenant? tenant) => throw new NotSupportedException();
    }
}

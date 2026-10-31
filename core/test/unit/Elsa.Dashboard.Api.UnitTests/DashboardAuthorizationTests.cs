using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa.Authorization;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using Elsa.Dashboard.Api.Permissions;
using Elsa.Dashboard.Api.Services;
using Elsa.Testing.Shared.Authorization;
using Elsa.Workflows.Api.Permissions;
using Microsoft.Extensions.DependencyInjection;
using DashboardApiFeature = Elsa.Dashboard.Api.Features.DashboardApiFeature;
using static Elsa.Dashboard.Api.UnitTests.TestContributor;

namespace Elsa.Dashboard.Api.UnitTests;

/// <summary>
/// Elsa Studio shows the dashboard to every signed-in user and gates each widget by the permission of the data it shows,
/// so the API applies the same rule: <c>dashboard:view</c> reads everything, a narrower permission reads just its own
/// data, and the overview never refuses a caller who can read part of it.
/// </summary>
[Collection(nameof(EndpointSecurityCollection))]
public class DashboardAuthorizationTests : IAsyncLifetime
{
    private static readonly string WholeOverviewPermission = Format(WholeOverview);
    private static readonly string InstancesPermission = Format(InstancesView);

    private static readonly (HttpMethod Method, string Path)[] InstanceEndpointList =
    [
        (HttpMethod.Post, "/dashboard/workflow-trends"),
        (HttpMethod.Get, "/dashboard/recent-activity"),
        (HttpMethod.Get, "/dashboard/needs-attention"),
        (HttpMethod.Post, "/dashboard/workflow-hotspots")
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly (HttpMethod Method, string Path) OverviewEndpoint = (HttpMethod.Get, "/dashboard/overview");

    public static readonly TheoryData<string, string> InstanceEndpoints = ToTheoryData(InstanceEndpointList);
    public static readonly TheoryData<string, string> AllEndpoints = ToTheoryData(InstanceEndpointList.Append(OverviewEndpoint));

    public static readonly TheoryData<Type> InstanceEndpointTypes = new(
        typeof(Endpoints.Dashboard.WorkflowTrends.Endpoint),
        typeof(Endpoints.Dashboard.RecentActivity.Endpoint),
        typeof(Endpoints.Dashboard.NeedsAttention.Endpoint),
        typeof(Endpoints.Dashboard.WorkflowHotspots.Endpoint));

    /// <summary>Every instance endpoint crossed with each permission that reads instance data.</summary>
    public static readonly TheoryData<string, string, string> InstanceEndpointsWithInstanceAccess = ToTheoryData(
        from endpoint in InstanceEndpointList
        from permission in new[] { InstancesPermission, WholeOverviewPermission }
        select (endpoint.Method, endpoint.Path, permission));

    /// <summary>Every instance endpoint crossed with each permission that guards only another section.</summary>
    public static readonly TheoryData<string, string, string> InstanceEndpointsWithAnotherSectionsPermission = ToTheoryData(
        from endpoint in InstanceEndpointList
        from permission in new[] { RuntimeView, StructuredLogsView, ConsoleLogsView }.Select(Format)
        select (endpoint.Method, endpoint.Path, permission));

    /// <summary>The sections a caller holding only the permission reads; every other section is withheld.</summary>
    public static readonly TheoryData<string, SectionAccess> ReadableSections = SectionAccess.BySinglePermission.Select(x => (Format(x.Permission), x.Readable))
        .Concat([
            (WholeOverviewPermission, SectionAccess.All),
            ("workflows/*:view", new SectionAccess { Runtime = true, Instances = true }),
            ("*:view", SectionAccess.All)
        ])
        .ToTheoryData();

    private AuthorizationTestHost _host = null!;

    public async Task InitializeAsync() => _host = await AuthorizationTestHost.StartAsync<DashboardApiFeature>(
        endpoint => endpoint.Namespace?.StartsWith("Elsa.Dashboard.Api.Endpoints", StringComparison.Ordinal) == true,
        services => services.AddSingleton<IDashboardProvider>(new DefaultDashboardProvider([Declared()], new(new TestClock(DateTimeOffset.UtcNow)), new TestHostEnvironment())));

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task AnonymousCaller_IsRejected(string method, string path)
    {
        var response = await SendAsync(new(method), path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ReadableSections))]
    public async Task Overview_WithOnePermission_ReturnsOnlyTheSectionsItGuards(string permission, SectionAccess readable)
    {
        var response = await SendAsync(HttpMethod.Get, OverviewEndpoint.Path, permission);
        var overview = await ReadOverviewAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        readable.AssertOn(overview);
    }

    [Fact]
    public async Task Overview_WithAnyAuthenticatedCallerWithoutPermissions_WithholdsEverySection()
    {
        var response = await SendAsync(HttpMethod.Get, OverviewEndpoint.Path, authenticated: true);
        var overview = await ReadOverviewAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SectionAccess.None.AssertOn(overview);
        Assert.Null(overview.BackendName);
        Assert.Null(overview.EnvironmentName);
        Assert.Empty(overview.Metrics);
        Assert.Empty(overview.Panels);
    }

    [Fact]
    public async Task Overview_WithInstancesView_ReturnsTheMetricsAndPanelsBuiltFromInstances()
    {
        var overview = await ReadOverviewAsync(await SendAsync(HttpMethod.Get, OverviewEndpoint.Path, InstancesPermission));

        Assert.Equal([InstancesMetric], overview.Metrics.Select(x => x.Id));
        Assert.Equal([InstancesPanel], overview.Panels.Select(x => x.Id));
    }

    [Fact]
    public async Task Overview_WithDashboardView_ReturnsEverything()
    {
        var overview = await ReadOverviewAsync(await SendAsync(HttpMethod.Get, OverviewEndpoint.Path, WholeOverviewPermission));

        Assert.Equal(3, overview.Metrics.Count);
        Assert.Equal(3, overview.Panels.Count);
    }

    [Theory]
    [MemberData(nameof(InstanceEndpointsWithInstanceAccess))]
    public async Task InstanceEndpoint_WithInstancesViewOrDashboardView_IsAllowed(string method, string path, string permission)
    {
        var response = await SendAsync(new(method), path, permission);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(InstanceEndpointsWithAnotherSectionsPermission))]
    public async Task InstanceEndpoint_WithAPermissionOnlyForAnotherSection_IsForbidden(string method, string path, string permission)
    {
        var response = await SendAsync(new(method), path, permission);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InstanceEndpoints))]
    public async Task InstanceEndpoint_WithoutAnyPermission_IsForbidden(string method, string path)
    {
        var response = await SendAsync(new(method), path, authenticated: true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NeedsAttention_WithInstancesView_ReturnsOnlyTheFindingsBuiltFromInstances()
    {
        var response = await SendAsync(HttpMethod.Get, "/dashboard/needs-attention", InstancesPermission);
        var findings = (await response.Content.ReadFromJsonAsync<DashboardNeedsAttentionResponse>(JsonOptions))!.Findings;

        Assert.Equal([InstancesFinding], findings.Select(x => x.Id));
    }

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task CallerWithoutPermissions_WithSecurityDisabled_IsAllowedEverything(string method, string path)
    {
        await _host.RestartWithSecurityDisabledAsync();

        var response = await SendAsync(new(method), path, authenticated: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Overview_WithSecurityDisabled_ReturnsEverything()
    {
        await _host.RestartWithSecurityDisabledAsync();

        var overview = await ReadOverviewAsync(await SendAsync(HttpMethod.Get, OverviewEndpoint.Path, authenticated: true));

        SectionAccess.All.AssertOn(overview);
        Assert.Equal(3, overview.Metrics.Count);
    }

    [Theory]
    [InlineData("GET", "/dashboard/overview", "metrics")]
    [InlineData("GET", "/dashboard/needs-attention", "findings")]
    [InlineData("POST", "/dashboard/workflow-trends", "buckets")]
    [InlineData("GET", "/dashboard/recent-activity", "items")]
    [InlineData("POST", "/dashboard/workflow-hotspots", "items")]
    public async Task Response_NeverSerializesThePermissionsThatGuardIt(string method, string path, string collection)
    {
        var response = await SendAsync(new(method), path, WholeOverviewPermission);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.NotEmpty(json.RootElement.GetProperty(collection).EnumerateArray());
        Assert.DoesNotContain("permission", json.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(InstanceEndpointTypes))]
    public void InstanceEndpoint_RecordsBothPermissionsThatOpenIt(Type endpointType)
    {
        var requirement = EndpointPermissionRegistry.FindRequirement(endpointType);

        Assert.NotNull(requirement);
        Assert.Equivalent(new[] { new Permission(DashboardResourcePermissions.Dashboard, CoreVerbs.View), new Permission(WorkflowPermissions.Instances, CoreVerbs.View) }, requirement.AnyOf, strict: true);
    }

    [Fact]
    public void DashboardAccess_PinsTheInstancesPermissionToTheWorkflowsModule()
    {
        Assert.Equal(new DashboardPermission(WorkflowPermissions.Instances, CoreVerbs.View), DashboardAccess.WorkflowInstances);
    }

    private static string Format(DashboardPermission permission) => new Permission(permission.Resource, permission.Verb).ToString();

    private static TheoryData<string, string> ToTheoryData(IEnumerable<(HttpMethod Method, string Path)> endpoints)
    {
        var data = new TheoryData<string, string>();

        foreach (var (method, path) in endpoints)
        {
            data.Add(method.Method, path);
        }

        return data;
    }

    private static TheoryData<string, string, string> ToTheoryData(IEnumerable<(HttpMethod Method, string Path, string Permission)> cases)
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (method, path, permission) in cases)
        {
            data.Add(method.Method, path, permission);
        }

        return data;
    }

    private static async Task<DashboardOverview> ReadOverviewAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<DashboardOverview>(JsonOptions))!;

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? permissions = null, bool authenticated = false) =>
        _host.SendAsync(method, path, permissions, authenticated, method == HttpMethod.Post ? JsonContent.Create(new { }) : null);
}

[CollectionDefinition(nameof(EndpointSecurityCollection), DisableParallelization = true)]
public class EndpointSecurityCollection;

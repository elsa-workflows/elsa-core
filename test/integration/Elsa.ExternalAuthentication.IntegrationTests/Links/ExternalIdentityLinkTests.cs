using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Links;

public partial class ExternalIdentityLinkTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private ITenantAccessor _tenant = null!;
    private TestConnectionRegistry _connections = null!;
    private bool _wasSecurityEnabled;

    protected HttpClient Client => _client!;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(ExternalAuthenticationFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.IdentityLinks";
        });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<MemoryStore<User>>();
        builder.Services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>();
        builder.Services.AddSingleton<ISystemClock, SystemClock>();
        builder.Services.AddSingleton<IExternalAuthenticationHandleHasher, HmacExternalAuthenticationHandleHasher>();
        builder.Services.AddSingleton<InMemoryExternalIdentityProvisionerState>();
        builder.Services.AddScoped<IUserStore, MemoryUserStore>();
        builder.Services.AddScoped<IUserProvider, StoreBasedUserProvider>();
        builder.Services.AddScoped<InMemoryExternalIdentityProvisioner>();
        builder.Services.AddScoped<IExternalIdentityProvisioner>(services => services.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        builder.Services.AddScoped<IExternalIdentityLinkManagementStore>(services => services.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        _connections = new TestConnectionRegistry();
        builder.Services.AddSingleton<IIdentityProviderConnectionRegistry>(_connections);
        _tenant = Substitute.For<ITenantAccessor>();
        _tenant.TenantId.Returns("tenant-a");
        builder.Services.AddSingleton(_tenant);
        builder.Services.AddScoped<ExternalIdentityLinkManagementService>();
        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(PermissionNames.ClaimType, PermissionNames.All), new Claim("sub", "admin")], "test"));
            await next(context);
        });
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();

        await SeedUserAsync("user-a", "alice", "tenant-a");
        await SeedUserAsync("user-b", "bob", "tenant-b");
        await SeedUserAsync("user-c", "charlie", "tenant-a");
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task PrelinkListAndUnlinkAreTenantBoundCursorPagedAndPolicyFallsBackAfterRemoval()
    {
        var first = await PrelinkAsync("subject-a");
        var second = await PrelinkAsync("subject-b");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var firstPage = await _client!.GetFromJsonAsync<LinkList>($"/external-authentication/identity-links?userId=user-a&pageSize=1");
        Assert.NotNull(firstPage);
        Assert.Single(firstPage!.Items);
        Assert.NotNull(firstPage.NextCursor);
        var serializedPage = JsonSerializer.Serialize(firstPage);
        Assert.DoesNotContain("subject-a", serializedPage);
        Assert.DoesNotContain("subjecthash", serializedPage, StringComparison.OrdinalIgnoreCase);

        var secondPage = await Client.GetFromJsonAsync<LinkList>($"/external-authentication/identity-links?userId=user-a&pageSize=1&cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");
        Assert.NotNull(secondPage);
        Assert.Single(secondPage!.Items);

        var link = await first.Content.ReadFromJsonAsync<LinkDocument>();
        var conflict = await PrelinkAsync("subject-a", "user-c");
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        _connections.Archived = true;
        var archived = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionId=connection-a");
        Assert.NotNull(archived);
        var archivedLinks = Assert.IsType<LinkList>(archived);
        Assert.Equal(2, archivedLinks.Items.Count);

        Assert.NotNull(link);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/external-authentication/identity-links/{link.Id}")).StatusCode);
        await using var scope = _app!.Services.CreateAsyncScope();
        var resolver = new DefaultExternalIdentityResolver(
            scope.ServiceProvider.GetRequiredService<IExternalIdentityProvisioner>(),
            [new RejectUnlinkedIdentityPolicy()],
            Microsoft.Extensions.Options.Options.Create(new Elsa.ExternalAuthentication.Options.ExternalAuthenticationOptions()));
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>());
        var connection = await _connections.FindByIdAsync("tenant-a", "connection-a");
        await Assert.ThrowsAsync<ExternalIdentityUnlinkedException>(() => resolver.ResolveAsync(new ExternalIdentityResolutionContext("tenant-a", connection!, identity, identity.Claims)).AsTask());
    }

    [Fact]
    public async Task TenantIsolationRejectsCrossTenantUsersAndDoesNotRevealTheirLinks()
    {
        Assert.Equal(HttpStatusCode.Created, (await PrelinkAsync("subject-a")).StatusCode);
        _tenant.TenantId.Returns("tenant-b");

        var crossTenantPrelink = await PrelinkAsync("subject-b", "user-a");
        Assert.Equal(HttpStatusCode.NotFound, crossTenantPrelink.StatusCode);
        var links = await _client!.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        Assert.NotNull(links);
        Assert.Empty(links!.Items);
    }

    [Fact]
    public async Task ConcurrentPrelinksForTheSameTupleConvergeOnOneLinkAndUser()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => PrelinkAsync("concurrent-subject")));
        Assert.All(responses, response => Assert.True(response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK));
        var links = await _client!.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionId=connection-a");
        Assert.NotNull(links);
        Assert.Single(links!.Items);
        Assert.Equal("user-a", Assert.Single(links.Items).UserId);
    }

    [Fact]
    public async Task RejectsMalformedOrOversizedCursorsAndPageSizes()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client!.GetAsync("/external-authentication/identity-links?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"/external-authentication/identity-links?cursor={new string('x', 513)}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/external-authentication/user-options?pageSize=51")).StatusCode);
    }

    [Fact]
    public async Task AllowsInheritedHostConnectionsButRejectsOtherTenantConnections()
    {
        _connections.UseHostConnection = true;
        Assert.Equal(HttpStatusCode.Created, (await PrelinkAsync("host-subject")).StatusCode);

        _connections.UseHostConnection = false;
        _tenant.TenantId.Returns("tenant-b");
        Assert.Equal(HttpStatusCode.NotFound, (await PrelinkAsync("other-tenant-subject", "user-b")).StatusCode);
    }

    private async Task<HttpResponseMessage> PrelinkAsync(string subject, string userId = "user-a") => await _client!.PostAsJsonAsync("/external-authentication/identity-links", new { userId, connectionId = "connection-a", issuer = "https://issuer.example/", subject });

    protected async Task SeedUserAsync(string id, string name, string tenantId)
    {
        await using var scope = _app!.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IUserStore>().SaveAsync(new User { Id = id, Name = name, TenantId = tenantId });
    }

    private sealed record LinkDocument(string Id, string UserId);
    private sealed record LinkList(IReadOnlyCollection<LinkDocument> Items, string? NextCursor);

    private sealed class TestConnectionRegistry : IIdentityProviderConnectionRegistry
    {
        public bool Archived { get; set; }
        public bool UseHostConnection { get; set; }

        public ValueTask<EffectiveConnectionRegistry> GetAsync(string targetTenantId, CancellationToken cancellationToken = default)
        {
            var connection = CreateConnection();
            return ValueTask.FromResult(new EffectiveConnectionRegistry([connection], [], "test"));
        }

        public ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default) => ValueTask.FromResult<EffectiveIdentityProviderConnection?>(string.Equals(targetTenantId, "tenant-a", StringComparison.Ordinal) && string.Equals(key, "contoso", StringComparison.Ordinal) ? CreateConnection() : null);
        public ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default) => ValueTask.FromResult<EffectiveIdentityProviderConnection?>(string.Equals(targetTenantId, "tenant-a", StringComparison.Ordinal) && string.Equals(connectionId, "connection-a", StringComparison.Ordinal) ? CreateConnection() : null);

        private EffectiveIdentityProviderConnection CreateConnection() => new(new IdentityProviderConnection
        {
            Id = "connection-a",
            TenantId = UseHostConnection ? ConnectionScope.HostTenantId : "tenant-a",
            Key = "contoso",
            AdapterType = "test",
            AdapterSettingsVersion = 1,
            DisplayName = "Contoso",
            ArchivedAt = Archived ? DateTimeOffset.UtcNow : null,
            IsEnabled = !Archived,
            ClaimProjection = ClaimProjection.Empty
        }, ConnectionSourceOwnership.Database, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test");
    }
}

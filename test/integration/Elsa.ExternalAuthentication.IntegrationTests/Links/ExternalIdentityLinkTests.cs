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
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Mediator.Contracts;
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
    private INotificationSender _notifications = null!;
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
        builder.Services.AddSingleton<ISystemClock>(new SteppingSystemClock(
            new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 26, 10, 1, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 26, 10, 2, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 26, 10, 3, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 26, 10, 4, 0, TimeSpan.Zero)));
        builder.Services.AddSingleton<IExternalAuthenticationHandleHasher, HmacExternalAuthenticationHandleHasher>();
        builder.Services.AddSingleton<InMemoryExternalIdentityProvisionerState>();
        builder.Services.AddScoped<IUserStore, MemoryUserStore>();
        builder.Services.AddScoped<IUserProvider, StoreBasedUserProvider>();
        builder.Services.AddSingleton<IRoleProvider>(Substitute.For<IRoleProvider>());
        builder.Services.AddScoped<InMemoryExternalIdentityProvisioner>();
        builder.Services.AddScoped<IExternalIdentityProvisioner>(services => services.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        builder.Services.AddScoped<IExternalIdentityLinkManagementStore>(services => services.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        _connections = new TestConnectionRegistry();
        builder.Services.AddSingleton<IIdentityProviderConnectionRegistry>(_connections);
        _tenant = Substitute.For<ITenantAccessor>();
        _tenant.TenantId.Returns("tenant-a");
        builder.Services.AddSingleton(_tenant);
        _notifications = Substitute.For<INotificationSender>();
        builder.Services.AddSingleton(_notifications);
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
        var archived = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionKey=contoso");
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
        var connection = await _connections.FindByKeyAsync("tenant-a", "contoso");
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
        var links = await _client!.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionKey=contoso");
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

    [Fact]
    public async Task ReplaceCreatesANewLinkAndResetsLifecycleMetadata()
    {
        var prelinked = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();

        var response = await ReplaceAsync(prelinked!.Id, "subject-new", "user-c", "fabrikam", "https://replacement.example/path/");
        var replacement = await response.Content.ReadFromJsonAsync<LinkDocument>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(replacement);
        Assert.NotEqual(prelinked.Id, replacement!.Id);
        Assert.Equal("user-c", replacement.UserId);
        Assert.Equal("fabrikam", replacement.ConnectionKey);
        Assert.Equal("https://replacement.example/path", replacement.Issuer);
        Assert.True(replacement.CreatedAt > prelinked.CreatedAt);
        Assert.Null(replacement.LastSignedInAt);

        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        var persisted = Assert.Single(links!.Items);
        Assert.Equal(replacement.Id, persisted.Id);

        await using var scope = _app!.Services.CreateAsyncScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IExternalIdentityProvisioner>();
        Assert.Null(await provisioner.FindLinkAsync("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims)));
        Assert.Equal(replacement.Id, (await provisioner.FindLinkAsync("tenant-a", "fabrikam", new ExternalIdentity("https://replacement.example/path", "subject-new", EmptyClaims)))!.Id);
    }

    [Fact]
    public async Task ReplaceConflictLeavesTheOldLinkUntouchedEvenWhenTheTupleBelongsToTheSameUser()
    {
        var old = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();
        var conflicting = await (await PrelinkAsync("subject-conflict")).Content.ReadFromJsonAsync<LinkDocument>();

        var response = await ReplaceAsync(old!.Id, "subject-conflict");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        Assert.Equal(2, links!.Items.Count);
        Assert.Contains(links.Items, x => x.Id == old.Id && x.UserId == old.UserId && x.ConnectionKey == old.ConnectionKey);
        Assert.Contains(links.Items, x => x.Id == conflicting!.Id);
    }

    [Fact]
    public async Task ReplaceUsesTheOldIdAsATenantBoundConcurrencyGuard()
    {
        var old = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();

        _tenant.TenantId.Returns("tenant-b");
        Assert.Equal(HttpStatusCode.NotFound, (await ReplaceAsync(old!.Id, "cross-tenant-subject", "user-b")).StatusCode);
        _tenant.TenantId.Returns("tenant-a");

        var responses = await Task.WhenAll(
            ReplaceAsync(old.Id, "winner-a"),
            ReplaceAsync(old.Id, "winner-b"));

        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        var missing = Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NotFound);
        Assert.Equal("not_found", (await missing.Content.ReadFromJsonAsync<ErrorDocument>())!.Error);
        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        Assert.Single(links!.Items);
    }

    [Fact]
    public async Task ReplaceAuditsSuccessAndConflictButNotValidationFailures()
    {
        var old = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();
        var conflicting = await (await PrelinkAsync("subject-conflict", "user-c")).Content.ReadFromJsonAsync<LinkDocument>();
        _notifications.ClearReceivedCalls();

        var successfulResponse = await ReplaceAsync(old!.Id, "subject-new", "user-c", "fabrikam");
        var replacement = await successfulResponse.Content.ReadFromJsonAsync<LinkDocument>();
        var replacementToConflict = await (await PrelinkAsync("subject-another")).Content.ReadFromJsonAsync<LinkDocument>();
        Assert.Equal(HttpStatusCode.Conflict, (await ReplaceAsync(replacementToConflict!.Id, "subject-conflict", "user-c")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ReplaceAsync(replacement!.Id, "subject-invalid", issuer: "http://issuer.example")).StatusCode);

        var notifications = _notifications.ReceivedCalls()
            .Select(x => x.GetArguments()[0])
            .OfType<ExternalIdentityLinkReplaced>()
            .ToArray();
        Assert.Collection(
            notifications,
            succeeded =>
            {
                Assert.Equal(SecurityEventOutcome.Succeeded, succeeded.Context.Outcome);
                Assert.Equal("admin", succeeded.Context.ActorId);
                Assert.Equal("tenant-a", succeeded.Context.TenantId);
                Assert.Equal(old.Id, succeeded.OldLinkId);
                Assert.Equal(replacement.Id, succeeded.NewLinkId);
                Assert.Equal("user-a", succeeded.OldUserId);
                Assert.Equal("user-c", succeeded.NewUserId);
                Assert.Equal("contoso", succeeded.OldConnectionKey);
                Assert.Equal("fabrikam", succeeded.NewConnectionKey);
                Assert.Null(succeeded.ConflictingLinkId);
            },
            failed =>
            {
                Assert.Equal(SecurityEventOutcome.Failed, failed.Context.Outcome);
                Assert.Equal(replacementToConflict.Id, failed.OldLinkId);
                Assert.Null(failed.NewLinkId);
                Assert.Equal(conflicting!.Id, failed.ConflictingLinkId);
                Assert.Equal("user-c", failed.ConflictingUserId);
                Assert.Equal("contoso", failed.ConflictingConnectionKey);
            });
    }

    private async Task<HttpResponseMessage> PrelinkAsync(string subject, string userId = "user-a") => await _client!.PostAsJsonAsync("/external-authentication/identity-links", new { userId, connectionKey = "contoso", issuer = "https://issuer.example/", subject });
    private async Task<HttpResponseMessage> ReplaceAsync(string linkId, string subject, string userId = "user-a", string connectionKey = "contoso", string issuer = "https://issuer.example/") => await _client!.PostAsJsonAsync($"/external-authentication/identity-links/{linkId}/replace", new { userId, connectionKey, issuer, subject });

    protected async Task SeedUserAsync(string id, string name, string tenantId)
    {
        await using var scope = _app!.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IUserStore>().SaveAsync(new User { Id = id, Name = name, TenantId = tenantId });
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyClaims { get; } = new Dictionary<string, IReadOnlyCollection<string>>();

    private sealed record LinkDocument(string Id, string UserId, string ConnectionKey, string Issuer, DateTimeOffset CreatedAt, DateTimeOffset? LastSignedInAt);
    private sealed record LinkList(IReadOnlyCollection<LinkDocument> Items, string? NextCursor);
    private sealed record ErrorDocument(string Error, string Message);

    private sealed class TestConnectionRegistry : IIdentityProviderConnectionRegistry
    {
        public bool Archived { get; set; }
        public bool UseHostConnection { get; set; }

        public ValueTask<EffectiveConnectionRegistry> GetAsync(string targetTenantId, CancellationToken cancellationToken = default)
        {
            var connection = CreateConnection();
            return ValueTask.FromResult(new EffectiveConnectionRegistry([connection], [], "test"));
        }

        public ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default) => ValueTask.FromResult<EffectiveIdentityProviderConnection?>(string.Equals(targetTenantId, "tenant-a", StringComparison.Ordinal) && (string.Equals(key, "contoso", StringComparison.Ordinal) || string.Equals(key, "fabrikam", StringComparison.Ordinal)) ? CreateConnection(key) : null);
        public ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default) => ValueTask.FromResult<EffectiveIdentityProviderConnection?>(string.Equals(targetTenantId, "tenant-a", StringComparison.Ordinal) && string.Equals(connectionId, "connection-a", StringComparison.Ordinal) ? CreateConnection() : null);

        private EffectiveIdentityProviderConnection CreateConnection(string key = "contoso") => new(new IdentityProviderConnection
        {
            Id = $"connection-{key}",
            TenantId = UseHostConnection ? ConnectionScope.HostTenantId : "tenant-a",
            Key = key,
            AdapterType = "test",
            AdapterSettingsVersion = 1,
            DisplayName = "Contoso",
            ArchivedAt = Archived ? DateTimeOffset.UtcNow : null,
            IsEnabled = !Archived,
            ClaimProjection = ClaimProjection.Empty
        }, ConnectionSourceOwnership.Database, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test");
    }

    private sealed class SteppingSystemClock(params DateTimeOffset[] instants) : ISystemClock
    {
        private int _index;
        public DateTimeOffset UtcNow => instants[Math.Min(_index++, instants.Length - 1)];
    }
}

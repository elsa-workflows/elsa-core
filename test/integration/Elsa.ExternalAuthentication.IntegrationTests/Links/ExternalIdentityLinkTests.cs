using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
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
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Links;

public partial class ExternalIdentityLinkTests : WebApplicationTest<ExternalIdentityLinkWebApplicationFactory, ExternalAuthenticationTestEntryPoint>
{
    private HttpClient? _client;
    private readonly TestAuthenticationState _authentication = new();
    private readonly MemoryStore<User> _users = new();
    private readonly ITenantAccessor _tenant = Substitute.For<ITenantAccessor>();
    private readonly TestConnectionRegistry _connections = new();
    private readonly INotificationSender _notifications = Substitute.For<INotificationSender>();
    private readonly ISystemClock _clock = new SteppingSystemClock(
        new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 26, 10, 1, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 26, 10, 2, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 26, 10, 3, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 26, 10, 4, 0, TimeSpan.Zero));

    protected HttpClient Client => _client ??= Factory.CreateClient();

    public ExternalIdentityLinkTests()
    {
        _authentication.SetClaims(
            new Claim(PermissionNames.ClaimType, PermissionNames.All),
            new Claim("sub", "admin"));
        _tenant.TenantId.Returns("tenant-a");
        _users.SaveMany(
        [
            new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" },
            new User { Id = "user-b", Name = "bob", TenantId = "tenant-b" },
            new User { Id = "user-c", Name = "charlie", TenantId = "tenant-a" }
        ], user => user.Id);
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton(_authentication);
        services.AddSingleton(_users);
        services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>();
        services.AddSingleton(_clock);
        services.AddSingleton<IExternalAuthenticationHandleHasher, HmacExternalAuthenticationHandleHasher>();
        services.AddSingleton<InMemoryExternalIdentityProvisionerState>();
        services.AddScoped<IUserStore, MemoryUserStore>();
        services.AddScoped<IUserProvider, StoreBasedUserProvider>();
        services.AddSingleton<IRoleProvider>(Substitute.For<IRoleProvider>());
        services.AddScoped<InMemoryExternalIdentityProvisioner>();
        services.AddScoped<IExternalIdentityProvisioner>(provider => provider.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        services.AddScoped<IExternalIdentityLinkManagementStore>(provider => provider.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        services.AddSingleton<IIdentityProviderConnectionRegistry>(_connections);
        services.AddSingleton(_tenant);
        services.AddSingleton(_notifications);
        services.AddScoped<ExternalIdentityLinkManagementService>();
    }

    [Test]
    public async Task PrelinkListAndUnlinkAreTenantBoundCursorPagedAndPolicyFallsBackAfterRemoval()
    {
        var first = await PrelinkAsync("subject-a");
        var second = await PrelinkAsync("subject-b");
        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var firstPage = await Client.GetFromJsonAsync<LinkList>($"/external-authentication/identity-links?userId=user-a&pageSize=1");
        await Assert.That(firstPage).IsNotNull();
        await Assert.That(firstPage!.Items).HasSingleItem();
        await Assert.That(firstPage.NextCursor).IsNotNull();
        var serializedPage = JsonSerializer.Serialize(firstPage);
        await Assert.That(serializedPage).DoesNotContain("subject-a");
        await Assert.That(serializedPage).DoesNotContain("subjecthash").WithComparison(StringComparison.OrdinalIgnoreCase);

        var secondPage = await Client.GetFromJsonAsync<LinkList>($"/external-authentication/identity-links?userId=user-a&pageSize=1&cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");
        await Assert.That(secondPage).IsNotNull();
        await Assert.That(secondPage!.Items).HasSingleItem();

        var link = await first.Content.ReadFromJsonAsync<LinkDocument>();
        var conflict = await PrelinkAsync("subject-a", "user-c");
        await Assert.That(conflict.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

        _connections.Archived = true;
        var archived = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionKey=contoso");
        await Assert.That(archived).IsNotNull();
        var archivedLinksValue1 = archived;
        await Assert.That(archivedLinksValue1).IsOfType(typeof(LinkList));
        var archivedLinks = (LinkList)archivedLinksValue1!;
        await Assert.That(archivedLinks.Items.Count).IsEqualTo(2);

        await Assert.That(link).IsNotNull();
        await Assert.That((await Client.DeleteAsync($"/external-authentication/identity-links/{link.Id}")).StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await using var scope = Services.CreateAsyncScope();
        var resolver = new DefaultExternalIdentityResolver(
            scope.ServiceProvider.GetRequiredService<IExternalIdentityProvisioner>(),
            [new RejectUnlinkedIdentityPolicy()],
            Microsoft.Extensions.Options.Options.Create(new Elsa.ExternalAuthentication.Options.ExternalAuthenticationOptions()));
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>());
        var connection = await _connections.FindByKeyAsync("tenant-a", "contoso");
        await Assert.ThrowsExactlyAsync<ExternalIdentityUnlinkedException>(() => resolver.ResolveAsync(new ExternalIdentityResolutionContext("tenant-a", connection!, identity, identity.Claims)).AsTask());
    }

    [Test]
    public async Task TenantIsolationRejectsCrossTenantUsersAndDoesNotRevealTheirLinks()
    {
        await Assert.That((await PrelinkAsync("subject-a")).StatusCode).IsEqualTo(HttpStatusCode.Created);
        _tenant.TenantId.Returns("tenant-b");

        var crossTenantPrelink = await PrelinkAsync("subject-b", "user-a");
        await Assert.That(crossTenantPrelink.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        await Assert.That(links).IsNotNull();
        await Assert.That(links!.Items).IsEmpty();
    }

    [Test]
    public async Task IdentityLinksApiReturnsTheRecordedSignInForTheCurrentTenantAndConnection()
    {
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims);
        var prelinked = await (await PrelinkAsync(identity.Subject)).Content.ReadFromJsonAsync<LinkDocument>();
        await Assert.That(prelinked!.LastSignedInAt).IsNull();
        var signedInAt = new DateTimeOffset(2026, 7, 26, 11, 0, 0, TimeSpan.Zero);
        await using (var scope = Services.CreateAsyncScope())
        {
            var tracker = scope.ServiceProvider.GetRequiredService<InMemoryExternalIdentityProvisioner>();
            await Assert.That(await tracker.RecordSuccessfulSignInAsync("tenant-a", "contoso", identity, "user-a", signedInAt)).IsTrue();
        }

        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionKey=contoso");
        var link = (await Assert.That(links!.Items).HasSingleItem())!;
        await Assert.That(link.Id).IsEqualTo(prelinked.Id);
        await Assert.That(link.LastSignedInAt).IsEqualTo(signedInAt);
        await Assert.That((await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionKey=fabrikam"))!.Items).IsEmpty();

        _tenant.TenantId.Returns("tenant-b");
        await Assert.That((await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionKey=contoso"))!.Items).IsEmpty();
    }

    [Test]
    public async Task ConcurrentPrelinksForTheSameTupleConvergeOnOneLinkAndUser()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => PrelinkAsync("concurrent-subject")));
        foreach (var response in responses)
            await Assert.That(response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK).IsTrue();
        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links?connectionKey=contoso");
        await Assert.That(links).IsNotNull();
        await Assert.That(links!.Items).HasSingleItem();
        await Assert.That((await Assert.That(links.Items).HasSingleItem())!.UserId).IsEqualTo("user-a");
    }

    [Test]
    public async Task RejectsMalformedOrOversizedCursorsAndPageSizes()
    {
        await Assert.That((await Client.GetAsync("/external-authentication/identity-links?pageSize=101")).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That((await Client.GetAsync($"/external-authentication/identity-links?cursor={new string('x', 513)}")).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That((await Client.GetAsync("/external-authentication/user-options?pageSize=51")).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AllowsInheritedHostConnectionsButRejectsOtherTenantConnections()
    {
        _connections.UseHostConnection = true;
        await Assert.That((await PrelinkAsync("host-subject")).StatusCode).IsEqualTo(HttpStatusCode.Created);

        _connections.UseHostConnection = false;
        _tenant.TenantId.Returns("tenant-b");
        await Assert.That((await PrelinkAsync("other-tenant-subject", "user-b")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ManualLinkManagementAllowsDisabledAndInvalidEffectiveConnections()
    {
        _connections.IsEnabled = false;
        _connections.Validity = ConnectionValidity.Invalid;

        var prelinked = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();
        var response = await ReplaceAsync(prelinked!.Id, "subject-new");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That((await response.Content.ReadFromJsonAsync<LinkDocument>())!.Id).IsNotEqualTo(prelinked.Id);

        _connections.Archived = true;
        await Assert.That((await PrelinkAsync("archived-subject")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ReplaceCreatesANewLinkAndResetsLifecycleMetadata()
    {
        var prelinked = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();

        var response = await ReplaceAsync(prelinked!.Id, "subject-new", "user-c", "fabrikam", "https://replacement.example/path/");
        var replacement = await response.Content.ReadFromJsonAsync<LinkDocument>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(replacement).IsNotNull();
        await Assert.That(replacement!.Id).IsNotEqualTo(prelinked.Id);
        await Assert.That(replacement.UserId).IsEqualTo("user-c");
        await Assert.That(replacement.ConnectionKey).IsEqualTo("fabrikam");
        await Assert.That(replacement.Issuer).IsEqualTo("https://replacement.example/path");
        await Assert.That(replacement.CreatedAt > prelinked.CreatedAt).IsTrue();
        await Assert.That(replacement.LastSignedInAt).IsNull();

        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        var persisted = (await Assert.That(links!.Items).HasSingleItem())!;
        await Assert.That(persisted.Id).IsEqualTo(replacement.Id);

        await using var scope = Services.CreateAsyncScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IExternalIdentityProvisioner>();
        await Assert.That(await provisioner.FindLinkAsync("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims))).IsNull();
        await Assert.That((await provisioner.FindLinkAsync("tenant-a", "fabrikam", new ExternalIdentity("https://replacement.example/path", "subject-new", EmptyClaims)))!.Id).IsEqualTo(replacement.Id);
    }

    [Test]
    public async Task ReplaceConflictLeavesTheOldLinkUntouchedEvenWhenTheTupleBelongsToTheSameUser()
    {
        var old = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();
        var conflicting = await (await PrelinkAsync("subject-conflict")).Content.ReadFromJsonAsync<LinkDocument>();

        var response = await ReplaceAsync(old!.Id, "subject-conflict");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        await Assert.That(links!.Items.Count).IsEqualTo(2);
        await Assert.That(links.Items).Contains(x => x.Id == old.Id && x.UserId == old.UserId && x.ConnectionKey == old.ConnectionKey);
        await Assert.That(links.Items).Contains(x => x.Id == conflicting!.Id);
    }

    [Test]
    public async Task ReplaceUsesTheOldIdAsATenantBoundConcurrencyGuard()
    {
        var old = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();

        _tenant.TenantId.Returns("tenant-b");
        await Assert.That((await ReplaceAsync(old!.Id, "cross-tenant-subject", "user-b")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _tenant.TenantId.Returns("tenant-a");

        var responses = await Task.WhenAll(
            ReplaceAsync(old.Id, "winner-a"),
            ReplaceAsync(old.Id, "winner-b"));

        await Assert.That(responses).HasSingleItem(x => x.StatusCode == HttpStatusCode.Created);
        var missing = (await Assert.That(responses).HasSingleItem(x => x.StatusCode == HttpStatusCode.NotFound))!;
        await Assert.That((await missing.Content.ReadFromJsonAsync<ErrorDocument>())!.Error).IsEqualTo("not_found");
        var links = await Client.GetFromJsonAsync<LinkList>("/external-authentication/identity-links");
        await Assert.That(links!.Items).HasSingleItem();
    }

    [Test]
    public async Task ReplaceAuditsSuccessAndConflictButNotValidationFailures()
    {
        var old = await (await PrelinkAsync("subject-old")).Content.ReadFromJsonAsync<LinkDocument>();
        var conflicting = await (await PrelinkAsync("subject-conflict", "user-c")).Content.ReadFromJsonAsync<LinkDocument>();
        _notifications.ClearReceivedCalls();

        var successfulResponse = await ReplaceAsync(old!.Id, "subject-new", "user-c", "fabrikam");
        var replacement = await successfulResponse.Content.ReadFromJsonAsync<LinkDocument>();
        var replacementToConflict = await (await PrelinkAsync("subject-another")).Content.ReadFromJsonAsync<LinkDocument>();
        await Assert.That((await ReplaceAsync(replacementToConflict!.Id, "subject-conflict", "user-c")).StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That((await ReplaceAsync(replacement!.Id, "subject-invalid", issuer: "http://issuer.example")).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var notifications = _notifications.ReceivedCalls()
            .Select(x => x.GetArguments()[0])
            .OfType<ExternalIdentityLinkReplaced>()
            .ToArray();
        await Assert.That(notifications.Length).IsEqualTo(2);
        var succeeded = notifications[0];
        await Assert.That(succeeded.Context.Outcome).IsEqualTo(SecurityEventOutcome.Succeeded);
        await Assert.That(succeeded.Context.ActorId).IsEqualTo("admin");
        await Assert.That(succeeded.Context.TenantId).IsEqualTo("tenant-a");
        await Assert.That(succeeded.OldLinkId).IsEqualTo(old.Id);
        await Assert.That(succeeded.NewLinkId).IsEqualTo(replacement.Id);
        await Assert.That(succeeded.OldUserId).IsEqualTo("user-a");
        await Assert.That(succeeded.NewUserId).IsEqualTo("user-c");
        await Assert.That(succeeded.OldConnectionKey).IsEqualTo("contoso");
        await Assert.That(succeeded.NewConnectionKey).IsEqualTo("fabrikam");
        await Assert.That(succeeded.ConflictingLinkId).IsNull();

        var failed = notifications[1];
        await Assert.That(failed.Context.Outcome).IsEqualTo(SecurityEventOutcome.Failed);
        await Assert.That(failed.OldLinkId).IsEqualTo(replacementToConflict.Id);
        await Assert.That(failed.NewLinkId).IsNull();
        await Assert.That(failed.ConflictingLinkId).IsEqualTo(conflicting!.Id);
        await Assert.That(failed.ConflictingUserId).IsEqualTo("user-c");
        await Assert.That(failed.ConflictingConnectionKey).IsEqualTo("contoso");
    }

    private async Task<HttpResponseMessage> PrelinkAsync(string subject, string userId = "user-a") => await Client.PostAsJsonAsync("/external-authentication/identity-links", new { userId, connectionKey = "contoso", issuer = "https://issuer.example/", subject });
    private async Task<HttpResponseMessage> ReplaceAsync(string linkId, string subject, string userId = "user-a", string connectionKey = "contoso", string issuer = "https://issuer.example/") => await Client.PostAsJsonAsync($"/external-authentication/identity-links/{linkId}/replace", new { userId, connectionKey, issuer, subject });

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyClaims { get; } = new Dictionary<string, IReadOnlyCollection<string>>();

    private sealed record LinkDocument(string Id, string UserId, string ConnectionKey, string Issuer, DateTimeOffset CreatedAt, DateTimeOffset? LastSignedInAt);
    private sealed record LinkList(IReadOnlyCollection<LinkDocument> Items, string? NextCursor);
    private sealed record ErrorDocument(string Error, string Message);

    private sealed class TestConnectionRegistry : IIdentityProviderConnectionRegistry
    {
        public bool Archived { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool UseHostConnection { get; set; }
        public ConnectionValidity Validity { get; set; } = ConnectionValidity.Valid;

        public ValueTask<EffectiveConnectionRegistry> GetAsync(string targetTenantId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<EffectiveIdentityProviderConnection> effective =
                string.Equals(targetTenantId, "tenant-a", StringComparison.Ordinal) || UseHostConnection
                    ? [CreateConnection(), CreateConnection("fabrikam")]
                    : [];
            return ValueTask.FromResult(new EffectiveConnectionRegistry(effective, [], "test"));
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
            IsEnabled = IsEnabled,
            ClaimProjection = ClaimProjection.Empty
        }, ConnectionSourceOwnership.Database, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), Validity, false, "test");
    }

    private sealed class SteppingSystemClock(params DateTimeOffset[] instants) : ISystemClock
    {
        private int _index;
        public DateTimeOffset UtcNow => instants[Math.Min(_index++, instants.Length - 1)];
    }
}

using System.Text.Json;
using Elsa.Common.Services;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Services;
using Elsa.Identity.Providers;
using Elsa.Workflows;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.IntegrationTests.Identity;

public class JustInTimeProvisioningTests
{
    [Fact]
    public async Task ExistingLinkResolvesWithoutApplyingTheUnlinkedPolicy()
    {
        var provisioner = new AtomicProvisioner();
        provisioner.Seed("tenant-a", "tenant-a", "connection-a", "https://issuer.example", "subject-a", "user-a");
        var resolver = CreateResolver(provisioner, new RejectUnlinkedIdentityPolicy());

        var resolution = await resolver.ResolveAsync(CreateContext());

        Assert.Equal("user-a", resolution.UserId);
        Assert.False(resolution.WasProvisioned);
    }

    [Fact]
    public async Task RejectPolicyDeniesAnUnknownIdentityWithoutUsingMutableClaims()
    {
        var resolver = CreateResolver(new AtomicProvisioner(), new RejectUnlinkedIdentityPolicy());
        var context = CreateContext("reject", new Dictionary<string, IReadOnlyCollection<string>> { ["email"] = ["person@example.test"] });

        var exception = await Assert.ThrowsAsync<ExternalIdentityUnlinkedException>(() => resolver.ResolveAsync(context).AsTask());

        Assert.Equal("identity_unlinked", exception.SafeReason);
    }

    [Fact]
    public async Task ConcurrentJitRequestsConvergeOnOneCredentiallessTenantUserAndLink()
    {
        var provisioner = new AtomicProvisioner();
        var resolver = CreateResolver(provisioner, new CreateUserUnlinkedIdentityPolicy());
        var context = CreateContext();

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => resolver.ResolveAsync(context).AsTask()));

        Assert.Equal("user-1", Assert.Single(results.Select(x => x.UserId).Distinct()));
        Assert.Single(results, x => x.WasProvisioned);
        var user = Assert.Single(provisioner.Users);
        Assert.Equal("tenant-a", user.TenantId);
        Assert.Null(user.HashedPassword);
        Assert.Null(user.HashedPasswordSalt);
        var validator = new DefaultUserCredentialsValidator(new StaticUserProvider(user), new DefaultSecretHasher());
        Assert.Null(await validator.ValidateAsync(user.Name, "any-password"));
    }

    [Fact]
    public async Task ResolverRejectsALinkForAnotherTenant()
    {
        var provisioner = new AtomicProvisioner();
        provisioner.Seed("tenant-a", "tenant-b", "connection-a", "https://issuer.example", "subject-a", "user-b");
        var resolver = CreateResolver(provisioner, new RejectUnlinkedIdentityPolicy());

        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync(CreateContext()).AsTask());
    }

    [Fact]
    public async Task InMemoryProvisionerCreatesALinkForAnExistingUserOnlyInTheTargetTenant()
    {
        var userStore = new MemoryUserStore(new MemoryStore<User>());
        var user = new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" };
        await userStore.SaveAsync(user);
        var provisioner = CreateInMemoryProvisioner(userStore);
        var request = CreateProvisioningRequest(existingUserId: user.Id);

        var result = await provisioner.CreateLinkOrGetExistingAsync(request);
        var replay = await provisioner.CreateLinkOrGetExistingAsync(request);

        Assert.Equal(user.Id, result.UserId);
        Assert.False(result.WasCreated);
        Assert.Equal(result.Link.Id, replay.Link.Id);
        Assert.False(replay.WasCreated);
    }

    [Fact]
    public async Task InMemoryProvisionerRejectsLinkingAUserFromAnotherTenant()
    {
        var userStore = new MemoryUserStore(new MemoryStore<User>());
        await userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-b" });
        var provisioner = CreateInMemoryProvisioner(userStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.CreateLinkOrGetExistingAsync(CreateProvisioningRequest(existingUserId: "user-b")).AsTask());
    }

    [Fact]
    public async Task InMemoryProvisionerRetriesAGeneratedUserNameCollision()
    {
        var userStore = new MemoryUserStore(new MemoryStore<User>());
        await userStore.SaveAsync(new User { Id = "existing", Name = "external-collision", TenantId = "tenant-a" });
        var provisioner = CreateInMemoryProvisioner(userStore, new SequenceIdentityGenerator("collision", "available", "user-1", "link-1"));

        var result = await provisioner.CreateLinkOrGetExistingAsync(CreateProvisioningRequest());
        var user = await userStore.FindAsync(new Elsa.Identity.Models.UserFilter { Id = result.UserId });

        Assert.True(result.WasCreated);
        Assert.NotNull(user);
        Assert.Equal("external-available", user.Name);
        Assert.Null(user.HashedPassword);
        Assert.Null(user.HashedPasswordSalt);
    }

    [Fact]
    public async Task InMemoryProvisionerTreatsEachExternalIdentityTupleAsDistinct()
    {
        var userStore = new MemoryUserStore(new MemoryStore<User>());
        var provisioner = CreateInMemoryProvisioner(userStore);
        var first = await provisioner.CreateLinkOrGetExistingAsync(CreateProvisioningRequest());
        var differentIssuer = await provisioner.CreateLinkOrGetExistingAsync(CreateProvisioningRequest(issuer: "https://issuer-two.example"));
        var differentConnection = await provisioner.CreateLinkOrGetExistingAsync(CreateProvisioningRequest(connectionId: "connection-b"));

        Assert.Equal(3, new[] { first.Link.Id, differentIssuer.Link.Id, differentConnection.Link.Id }.Distinct().Count());
        Assert.Equal(3, new[] { first.UserId, differentIssuer.UserId, differentConnection.UserId }.Distinct().Count());
    }

    [Fact]
    public async Task InMemoryProvisionerSharesTupleStateAcrossDependencyInjectionScopes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MemoryStore<User>>();
        services.AddSingleton<IIdentityGenerator>(new Elsa.Workflows.GuidIdentityGenerator());
        services.AddSingleton<ISystemClock>(new FixedSystemClock(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero)));
        services.AddSingleton<IExternalAuthenticationHandleHasher, HmacExternalAuthenticationHandleHasher>();
        services.AddSingleton<InMemoryExternalIdentityProvisionerState>();
        services.AddScoped<IUserStore, MemoryUserStore>();
        services.AddScoped<IUserProvider, StoreBasedUserProvider>();
        services.AddScoped<IExternalIdentityProvisioner, InMemoryExternalIdentityProvisioner>();
        await using var provider = services.BuildServiceProvider();
        var request = CreateProvisioningRequest();

        ProvisioningResult created;
        await using (var firstScope = provider.CreateAsyncScope())
            created = await firstScope.ServiceProvider.GetRequiredService<IExternalIdentityProvisioner>().CreateLinkOrGetExistingAsync(request);

        await using var secondScope = provider.CreateAsyncScope();
        var provisioner = secondScope.ServiceProvider.GetRequiredService<IExternalIdentityProvisioner>();
        var converged = await provisioner.CreateLinkOrGetExistingAsync(request);
        var resolved = await provisioner.FindLinkAsync(request.TenantId, request.ConnectionId, request.Identity);

        Assert.Equal(created.Link.Id, converged.Link.Id);
        Assert.False(converged.WasCreated);
        Assert.Equal(created.Link.Id, resolved?.Id);
        Assert.Equal(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero), created.Link.CreatedAt);
    }

    private static DefaultExternalIdentityResolver CreateResolver(IExternalIdentityProvisioner provisioner, IUnlinkedIdentityPolicy policy) => new(
        provisioner,
        [policy],
        Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()));

    private static InMemoryExternalIdentityProvisioner CreateInMemoryProvisioner(IUserStore userStore, IIdentityGenerator? identityGenerator = null) => new(
        userStore,
        new StoreBasedUserProvider(userStore),
        identityGenerator ?? new Elsa.Workflows.GuidIdentityGenerator(),
        new Elsa.Common.Services.SystemClock(),
        new HmacExternalAuthenticationHandleHasher(),
        new InMemoryExternalIdentityProvisionerState());

    private static ProvisioningRequest CreateProvisioningRequest(string? existingUserId = null, string connectionId = "connection-a", string issuer = "https://issuer.example") => new(
        "tenant-a",
        connectionId,
        new ExternalIdentity(issuer, "subject-a", new Dictionary<string, IReadOnlyCollection<string>>()),
        existingUserId is null ? new UserCreationProposal("external") : null,
        existingUserId);

    private static ExternalIdentityResolutionContext CreateContext(string policyType = "create-user", IReadOnlyDictionary<string, IReadOnlyCollection<string>>? claims = null)
    {
        var connection = new IdentityProviderConnection
        {
            Id = "connection-a",
            TenantId = "tenant-a",
            Key = "contoso",
            AdapterType = "oidc",
            AdapterSettingsVersion = 1,
            DisplayName = "Contoso",
            IsEnabled = true,
            UnlinkedPolicy = new PolicySelection(policyType, 1, JsonSerializer.SerializeToElement(new { }))
        };
        var effectiveConnection = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test");
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", claims ?? new Dictionary<string, IReadOnlyCollection<string>>());

        return new ExternalIdentityResolutionContext("tenant-a", effectiveConnection, identity, identity.Claims);
    }

    private sealed class AtomicProvisioner : IExternalIdentityProvisioner
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<(string TenantId, string ConnectionId, string Issuer, string Subject), ProvisioningResult> _links = new();
        private readonly List<User> _users = [];

        public IReadOnlyCollection<User> Users => _users;

        public ValueTask<ExternalIdentityLink?> FindLinkAsync(string tenantId, string connectionId, ExternalIdentity identity, CancellationToken cancellationToken = default)
        {
            lock (_syncRoot)
                return ValueTask.FromResult(_links.GetValueOrDefault((tenantId, connectionId, identity.Issuer, identity.Subject))?.Link);
        }

        public ValueTask<ProvisioningResult> CreateLinkOrGetExistingAsync(ProvisioningRequest request, CancellationToken cancellationToken = default)
        {
            lock (_syncRoot)
            {
                var key = (request.TenantId, request.ConnectionId, request.Identity.Issuer, request.Identity.Subject);
                if (_links.TryGetValue(key, out var existing))
                    return ValueTask.FromResult(existing with { WasCreated = false });

                var user = new User { Id = $"user-{_users.Count + 1}", Name = $"external-{_users.Count + 1}", TenantId = request.TenantId };
                _users.Add(user);
                var link = new ExternalIdentityLink($"link-{_users.Count}", request.TenantId, request.ConnectionId, request.Identity.Issuer, "subject-hash", null, user.Id, DateTimeOffset.UtcNow, null);
                var result = new ProvisioningResult(user.Id, link, true, true);
                _links[key] = result;
                return ValueTask.FromResult(result);
            }
        }

        public void Seed(string lookupTenantId, string linkTenantId, string connectionId, string issuer, string subject, string userId)
        {
            lock (_syncRoot)
            {
                var link = new ExternalIdentityLink("link-seeded", linkTenantId, connectionId, issuer, "subject-hash", null, userId, DateTimeOffset.UtcNow, null);
                _links[(lookupTenantId, connectionId, issuer, subject)] = new ProvisioningResult(userId, link, false);
            }
        }
    }

    private sealed class StaticUserProvider(User user) : IUserProvider
    {
        public Task<User?> FindAsync(Elsa.Identity.Models.UserFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<User?>(filter.Name == user.Name ? user : null);
    }

    private sealed class SequenceIdentityGenerator(params string[] ids) : IIdentityGenerator
    {
        private readonly Queue<string> _ids = new(ids);
        public string GenerateId() => _ids.Dequeue();
    }

    private sealed class FixedSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}

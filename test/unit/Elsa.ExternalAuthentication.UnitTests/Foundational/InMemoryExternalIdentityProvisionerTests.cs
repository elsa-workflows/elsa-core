using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class InMemoryExternalIdentityProvisionerTests
{
    [Fact]
    public async Task RemovesLinkWhenUserDeletionWinsThePublicationRace()
    {
        var users = new MemoryUserStore(new MemoryStore<User>());
        await users.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        var provider = new DeleteAfterResolveUserProvider(new StoreBasedUserProvider(users), users);
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = new InMemoryExternalIdentityProvisioner(
            users,
            provider,
            Substitute.For<IRoleProvider>(),
            new GuidIdentityGenerator(),
            new TestSystemClock(DateTimeOffset.UtcNow),
            hasher,
            new InMemoryExternalIdentityProvisionerState());
        var request = new ProvisioningRequest(
            "tenant-a",
            "contoso",
            new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>()),
            null,
            "user-a");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        Assert.Null(await users.FindAsync(new UserFilter { Id = "user-a" }));
        Assert.Empty((await provisioner.FindAsync(new ExternalIdentityLinkFilter { TenantId = "tenant-a" })).Items);
    }

    [Fact]
    public async Task RemovesRestoredLinkWhenBothReplacementUsersAreDeleted()
    {
        var users = new MemoryUserStore(new MemoryStore<User>());
        await users.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await users.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var state = new InMemoryExternalIdentityProvisionerState();
        var originalProvisioner = CreateProvisioner(users, new StoreBasedUserProvider(users), hasher, state);
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>());
        var oldLink = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", identity, null, "user-a"))).Link;
        var racingProvider = new DeleteOnSelectedFindUserProvider(new StoreBasedUserProvider(users), users, 2, 3);
        var racingProvisioner = CreateProvisioner(users, racingProvider, hasher, state);

        await Assert.ThrowsAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                oldLink.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-b", new Dictionary<string, IReadOnlyCollection<string>>()))).AsTask());

        Assert.Null(await users.FindAsync(new UserFilter { Id = "user-a" }));
        Assert.Null(await users.FindAsync(new UserFilter { Id = "user-b" }));
        Assert.Empty((await racingProvisioner.FindAsync(new ExternalIdentityLinkFilter { TenantId = "tenant-a" })).Items);
    }

    private static InMemoryExternalIdentityProvisioner CreateProvisioner(
        IUserStore users,
        IUserProvider provider,
        IExternalAuthenticationHandleHasher hasher,
        InMemoryExternalIdentityProvisionerState state) => new(
        users,
        provider,
        Substitute.For<IRoleProvider>(),
        new GuidIdentityGenerator(),
        new TestSystemClock(DateTimeOffset.UtcNow),
        hasher,
        state);

    private sealed class DeleteAfterResolveUserProvider(IUserProvider inner, IUserStore users) : IUserProvider
    {
        private int _findCount;

        public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var user = await inner.FindAsync(filter, cancellationToken);
            if (user is not null && Interlocked.Increment(ref _findCount) == 1)
                await users.DeleteAsync(new UserFilter { Id = user.Id }, cancellationToken);
            return user;
        }
    }

    private sealed class DeleteOnSelectedFindUserProvider(IUserProvider inner, IUserStore users, params int[] deletionCounts) : IUserProvider
    {
        private readonly HashSet<int> _deletionCounts = deletionCounts.ToHashSet();
        private int _findCount;

        public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var user = await inner.FindAsync(filter, cancellationToken);
            if (user is not null && _deletionCounts.Contains(Interlocked.Increment(ref _findCount)))
            {
                await users.DeleteAsync(new UserFilter { Id = user.Id }, cancellationToken);
                return null;
            }

            return user;
        }
    }
}

using Elsa.Common.Services;
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
}

using Elsa.Testing.Shared.Multitenancy;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using NSubstitute;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationUserDeletionDependencyContributorTests
{
    [Fact]
    public async Task UserWithAnExternalIdentityLinkCannotBeDeleted()
    {
        var users = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
        await users.SaveAsync(new User { Id = "external-user", Name = "external-user", TenantId = "tenant-a" });
        var links = Substitute.For<IExternalIdentityLinkManagementStore>();
        links.FindAsync(
                Arg.Is<ExternalIdentityLinkFilter>(x => x.TenantId == "tenant-a" && x.UserId == "external-user"),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Page.Of<ExternalIdentityLink>([
                new ExternalIdentityLink("link-a", "tenant-a", "contoso", "https://issuer.example", "subject-hash", null, "external-user", DateTimeOffset.UtcNow, null)
            ], 1)));
        var coordinator = new UserDeletionCoordinator(
            users,
            [new ExternalAuthenticationUserDeletionDependencyContributor(links)]);

        var result = await coordinator.DeleteAsync("external-user");

        var blocked = Assert.IsType<UserDeletionOperationResult.Blocked>(result);
        Assert.Contains(blocked.Dependencies, x => x.Source == ExternalAuthenticationUserDeletionDependencyContributor.SourceName);
        Assert.NotNull(await users.FindAsync(new UserFilter { Id = "external-user" }));
    }

    [Fact]
    public async Task UserWithoutAnExternalIdentityLinkCanBeDeleted()
    {
        var users = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
        await users.SaveAsync(new User { Id = "local-user", Name = "local-user", TenantId = "tenant-a" });
        var links = Substitute.For<IExternalIdentityLinkManagementStore>();
        links.FindAsync(
                Arg.Is<ExternalIdentityLinkFilter>(x => x.TenantId == "tenant-a" && x.UserId == "local-user"),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Page.Of<ExternalIdentityLink>([], 0)));
        var coordinator = new UserDeletionCoordinator(
            users,
            [new ExternalAuthenticationUserDeletionDependencyContributor(links)]);

        var result = await coordinator.DeleteAsync("local-user");

        Assert.IsType<UserDeletionOperationResult.Deleted>(result);
        Assert.Null(await users.FindAsync(new UserFilter { Id = "local-user" }));
    }

    [Fact]
    public async Task UserIsRestoredWhenAnExternalIdentityLinkAppearsDuringDeletion()
    {
        var users = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
        await users.SaveAsync(new User { Id = "racing-user", Name = "racing-user", TenantId = "tenant-a" });
        var links = Substitute.For<IExternalIdentityLinkManagementStore>();
        var inspectionCount = 0;
        links.FindAsync(Arg.Any<ExternalIdentityLinkFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(Interlocked.Increment(ref inspectionCount) == 1
                ? Page.Of<ExternalIdentityLink>([], 0)
                : Page.Of<ExternalIdentityLink>([
                    new ExternalIdentityLink("link-a", "tenant-a", "contoso", "https://issuer.example", "subject-hash", null, "racing-user", DateTimeOffset.UtcNow, null)
                ], 1)));
        var coordinator = new UserDeletionCoordinator(
            users,
            [new ExternalAuthenticationUserDeletionDependencyContributor(links)]);

        var result = await coordinator.DeleteAsync("racing-user");

        Assert.IsType<UserDeletionOperationResult.Blocked>(result);
        Assert.NotNull(await users.FindAsync(new UserFilter { Id = "racing-user" }));
    }
}

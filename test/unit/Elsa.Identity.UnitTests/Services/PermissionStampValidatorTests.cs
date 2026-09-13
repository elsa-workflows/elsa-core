using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class PermissionStampValidatorTests : IDisposable
{
    private const string SharedUserName = "admin";

    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IUserProvider _userProvider = Substitute.For<IUserProvider>();
    private readonly IPermissionStampCalculator _calculator = Substitute.For<IPermissionStampCalculator>();

    private PermissionStampValidator CreateValidator(ITenantAccessor tenantAccessor) =>
        new(_userProvider, _calculator, _cache, tenantAccessor, MsOptions.Create(new PermissionStampOptions { IsEnabled = true }));

    public void Dispose() => _cache.Dispose();

    private static ClaimsPrincipal Principal(string stamp) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, SharedUserName),
                new Claim(PermissionStampCalculator.ClaimType, stamp)
            ],
            "test"));

    private void UseTenantUser(string tenantId, string stamp)
    {
        var user = new User { Id = $"{tenantId}-user", Name = SharedUserName, TenantId = tenantId };

        _userProvider
            .FindAsync(Arg.Is<UserFilter>(x => x.Name == SharedUserName && x.TenantId == tenantId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        _calculator.ComputeAsync(user, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(stamp));
    }

    [Test]
    public async Task OneTenantsCachedStampDoesNotSatisfyAnotherTenantsRevokedToken()
    {
        // User names are unique per tenant, not globally, so a cache keyed on the name alone would let
        // tenant A's lookup satisfy a revoked token belonging to tenant B's same-named user.
        UseTenantUser("tenant-a", "stamp-a");
        UseTenantUser("tenant-b", "stamp-b-current");

        var tenantA = CreateValidator(new TestTenantAccessor("tenant-a"));
        var tenantB = CreateValidator(new TestTenantAccessor("tenant-b"));

        // Prime the cache from tenant A.
        await Assert.That(await tenantA.IsCurrentAsync(Principal("stamp-a"))).IsTrue();

        // Tenant B presents a stamp that is no longer current for its own user.
        await Assert.That(await tenantB.IsCurrentAsync(Principal("stamp-b-revoked"))).IsFalse();
    }

    [Test]
    public async Task ACurrentStampIsAccepted()
    {
        UseTenantUser("tenant-a", "stamp-a");

        var validator = CreateValidator(new TestTenantAccessor("tenant-a"));

        await Assert.That(await validator.IsCurrentAsync(Principal("stamp-a"))).IsTrue();
    }

    [Test]
    public async Task AStaleStampIsRejected()
    {
        UseTenantUser("tenant-a", "stamp-current");

        var validator = CreateValidator(new TestTenantAccessor("tenant-a"));

        await Assert.That(await validator.IsCurrentAsync(Principal("stamp-stale"))).IsFalse();
    }

    [Test]
    public async Task ATokenWithoutAStampIsAccepted()
    {
        // Enabling the feature must not sign out everyone holding a token issued before it was on.
        UseTenantUser("tenant-a", "stamp-a");

        var validator = CreateValidator(new TestTenantAccessor("tenant-a"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, SharedUserName)], "test"));

        await Assert.That(await validator.IsCurrentAsync(principal)).IsTrue();
    }

    [Test]
    public async Task ValidationIsSkippedWhenDisabled()
    {
        var validator = new PermissionStampValidator(
            _userProvider, _calculator, _cache, new TestTenantAccessor("tenant-a"),
            MsOptions.Create(new PermissionStampOptions { IsEnabled = false }));

        await Assert.That(await validator.IsCurrentAsync(Principal("anything"))).IsTrue();
        await _userProvider.DidNotReceive().FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>());
    }
}
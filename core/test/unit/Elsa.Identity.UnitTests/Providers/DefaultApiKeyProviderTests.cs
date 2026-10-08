using AspNetCore.Authentication.ApiKey;
using Elsa;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Providers;

public class DefaultApiKeyProviderTests
{
    [Fact]
    public async Task AZeroGrantApplicationGetsTheEmptySetSentinel()
    {
        var apiKey = await ProvideAsync("noperm", permissions: []);

        Assert.NotNull(apiKey);
        Assert.Equal("client-noperm", apiKey.OwnerName);
        Assert.Equal(PermissionNames.None, Assert.Single(apiKey.Claims, x => x.Type == PermissionNames.ClaimType).Value);
    }

    [Fact]
    public async Task AGrantedApplicationKeepsItsPermissions()
    {
        var apiKey = await ProvideAsync("reader", permissions: ["identity/users:view", "identity/roles:view"]);

        Assert.NotNull(apiKey);
        Assert.Equal(
            new[] { "identity/users:view", "identity/roles:view" },
            apiKey.Claims.Where(x => x.Type == PermissionNames.ClaimType).Select(x => x.Value).ToArray());
        Assert.DoesNotContain(apiKey.Claims, x => x.Type == PermissionNames.ClaimType && x.Value == PermissionNames.None);
    }

    private static async Task<IApiKey?> ProvideAsync(string roleId, IReadOnlyCollection<string> permissions)
    {
        var application = new Application { ClientId = $"client-{roleId}", Roles = [roleId] };
        var validator = Substitute.For<IApplicationCredentialsValidator>();
        validator.ValidateAsync("key", Arg.Any<CancellationToken>()).Returns(application);

        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>())
            .Returns([new Role { Id = roleId, Name = roleId, Permissions = permissions.ToList() }]);

        return await new DefaultApiKeyProvider(validator, roles).ProvideAsync("key");
    }
}

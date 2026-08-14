using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultIdentityRefreshTokenServiceTests
{
    [Fact]
    public async Task RefreshAsyncRejectsAccessAndTamperedTokens()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions
        {
            SigningKey = IdentityTokenTestConstants.SigningKey,
            Issuer = "https://elsa.test",
            Audience = "elsa-api"
        });
        var tokenService = new DefaultElsaTokenService(new CurrentClock(), options);
        var user = new User { Id = "user-a", Name = "admin" };
        var context = new TokenIssuanceContext(user, [], [], []);
        var accessToken = await tokenService.IssueAccessTokenAsync(context);
        var refreshToken = await tokenService.IssueRefreshTokenAsync(context);
        var tamperedRefreshToken = refreshToken.Token[..^1] + (refreshToken.Token[^1] == 'a' ? 'b' : 'a');
        var accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();
        var service = new DefaultIdentityRefreshTokenService(
            Substitute.For<IUserProvider>(),
            accessTokenIssuer,
            new DefaultTenantAccessor(),
            options);

        Assert.Null(await service.RefreshAsync(accessToken.Token));
        Assert.Null(await service.RefreshAsync(tamperedRefreshToken));
        await accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private sealed class CurrentClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}

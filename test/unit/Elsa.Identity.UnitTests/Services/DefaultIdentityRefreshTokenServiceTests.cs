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
    private static readonly User User = new() { Id = "user-a", Name = "admin" };
    private static readonly IssuedTokens RefreshedTokens = new("access-b", "refresh-b");
    private readonly IdentityTokenOptions _tokenOptions = new()
    {
        SigningKey = IdentityTokenTestConstants.SigningKey,
        Issuer = "https://elsa.test",
        Audience = "elsa-api"
    };
    private readonly IAccessTokenIssuer _accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly IUserProvider _userProvider = Substitute.For<IUserProvider>();
    private readonly DefaultElsaTokenService _tokenService;
    private readonly DefaultIdentityRefreshTokenService _service;

    public DefaultIdentityRefreshTokenServiceTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(_tokenOptions);
        _userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Id == User.Id), Arg.Any<CancellationToken>()).Returns(User);
        _accessTokenIssuer.IssueTokensAsync(User, Arg.Any<CancellationToken>()).Returns(RefreshedTokens);
        _tokenService = new(new CurrentClock(), options);
        _service = new(_userProvider, _accessTokenIssuer, new DefaultTenantAccessor(), options);
    }

    [Fact]
    public async Task RefreshAsyncRejectsAccessAndTamperedTokens()
    {
        var context = new TokenIssuanceContext(User, [], [], []);
        var accessToken = await _tokenService.IssueAccessTokenAsync(context);
        var refreshToken = (await _tokenService.IssueRefreshTokenAsync(context)).Token;
        var tamperedRefreshToken = refreshToken[..^1] + (refreshToken[^1] == 'a' ? 'b' : 'a');

        Assert.Null(await _service.RefreshAsync(accessToken.Token));
        Assert.Null(await _service.RefreshAsync(tamperedRefreshToken));
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncIssuesTokensForTheUserNamedBySubject()
    {
        var refreshToken = (await _tokenService.IssueRefreshTokenAsync(new TokenIssuanceContext(User, [], [], []))).Token;

        Assert.Same(RefreshedTokens, await _service.RefreshAsync(refreshToken));
        await _accessTokenIssuer.Received(1).IssueTokensAsync(User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncRejectsATokenWhenTheUserIdNoLongerExistsEvenIfTheNameWasReused()
    {
        var replacement = new User { Id = "user-b", Name = User.Name };
        _userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Id == User.Id), Arg.Any<CancellationToken>()).Returns((User?)null);
        _userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Name == User.Name), Arg.Any<CancellationToken>()).Returns(replacement);
        _accessTokenIssuer.IssueTokensAsync(replacement, Arg.Any<CancellationToken>()).Returns(new IssuedTokens("access-c", "refresh-c"));
        var refreshToken = (await _tokenService.IssueRefreshTokenAsync(new TokenIssuanceContext(User, [], [], []))).Token;

        Assert.Null(await _service.RefreshAsync(refreshToken));
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncResolvesALegacyTokenWithoutSubjectByName()
    {
        _userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Name == User.Name), Arg.Any<CancellationToken>()).Returns(User);

        Assert.Same(RefreshedTokens, await _service.RefreshAsync(LegacyRefreshToken.CreateWithoutSubject(_tokenOptions, User)));
        await _accessTokenIssuer.Received(1).IssueTokensAsync(User, Arg.Any<CancellationToken>());
        await _userProvider.DidNotReceive().FindAsync(Arg.Is<UserFilter>(x => x.Id != null), Arg.Any<CancellationToken>());
    }

    private sealed class CurrentClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}

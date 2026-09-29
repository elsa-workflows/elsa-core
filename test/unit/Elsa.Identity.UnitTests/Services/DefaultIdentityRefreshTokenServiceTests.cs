using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
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
    private readonly IdentityTokenOptions _options = new()
    {
        SigningKey = IdentityTokenTestConstants.SigningKey,
        Issuer = "https://elsa.test",
        Audience = "elsa-api"
    };
    private readonly IAccessTokenIssuer _accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly DefaultElsaTokenService _tokenService;
    private readonly SessionRevoker _sessionRevoker;
    private readonly DefaultIdentityRefreshTokenService _service;

    public DefaultIdentityRefreshTokenServiceTests()
    {
        var clock = new MutableSystemClock();
        var options = Microsoft.Extensions.Options.Options.Create(_options);
        var userProvider = Substitute.For<IUserProvider>();
        userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Id == User.Id), Arg.Any<CancellationToken>()).Returns(User);
        _accessTokenIssuer.IssueTokensAsync(User, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(RefreshedTokens);
        _tokenService = new(clock, options);
        _sessionRevoker = new(new MemoryRevokedSessionStore(new MemoryStore<RevokedSession>()), clock, options);
        _service = new(userProvider, _accessTokenIssuer, new DefaultTenantAccessor(), _sessionRevoker, options);
    }

    [Fact]
    public async Task RefreshAsyncRejectsAccessAndTamperedTokens()
    {
        var context = new TokenIssuanceContext(User, [], [], []);
        var accessToken = await _tokenService.IssueAccessTokenAsync(context);
        var refreshToken = await _tokenService.IssueRefreshTokenAsync(context);
        var tamperedRefreshToken = refreshToken.Token[..^1] + (refreshToken.Token[^1] == 'a' ? 'b' : 'a');

        Assert.Null(await _service.RefreshAsync(accessToken.Token));
        Assert.Null(await _service.RefreshAsync(tamperedRefreshToken));
        await AssertNothingIssuedAsync();
    }

    [Fact]
    public async Task RefreshAsyncContinuesTheSessionOfTheRefreshToken()
    {
        var refreshToken = await IssueRefreshTokenAsync("session-a");

        Assert.Same(RefreshedTokens, await _service.RefreshAsync(refreshToken));
        await _accessTokenIssuer.Received(1).IssueTokensAsync(User, "session-a", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncRejectsARefreshTokenOfARevokedSession()
    {
        var refreshToken = await IssueRefreshTokenAsync("session-a");
        await _sessionRevoker.RevokeAsync("session-a", DateTimeOffset.UtcNow);

        Assert.Null(await _service.RefreshAsync(refreshToken));
        await AssertNothingIssuedAsync();
    }

    [Fact]
    public async Task RefreshAsyncCarriesALegacyRefreshTokenIntoASessionThatRevocationEnds()
    {
        var refreshToken = LegacyRefreshToken.Create(_options, User);
        var sessionId = SessionRevoker.GetSessionId(new ClaimsIdentity(), refreshToken);

        Assert.Same(RefreshedTokens, await _service.RefreshAsync(refreshToken));
        await _accessTokenIssuer.Received(1).IssueTokensAsync(User, sessionId, Arg.Any<CancellationToken>());

        await _sessionRevoker.RevokeAsync(sessionId, DateTimeOffset.UtcNow);

        Assert.Null(await _service.RefreshAsync(refreshToken));
    }

    private async Task<string> IssueRefreshTokenAsync(string sessionId) =>
        (await _tokenService.IssueRefreshTokenAsync(new TokenIssuanceContext(User, [], [], []) { SessionId = sessionId })).Token;

    // Both overloads: a check against only the one that is no longer called would pass without proving anything.
    private async Task AssertNothingIssuedAsync()
    {
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}

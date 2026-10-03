using System.Security.Claims;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultIdentityRefreshTokenServiceTests
{
    private static readonly User User = new() { Id = "user-a", Name = "admin" };
    private static readonly User Victim = new() { Id = "victim-id", Name = "victim" };
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
        _userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Id == Victim.Id), Arg.Any<CancellationToken>()).Returns(Victim);
        _accessTokenIssuer.IssueTokensAsync(User, Arg.Any<CancellationToken>()).Returns(RefreshedTokens);
        _accessTokenIssuer.IssueTokensAsync(Victim, Arg.Any<CancellationToken>()).Returns(new IssuedTokens("access-victim", "refresh-victim"));
        _tokenService = new(new CurrentClock(), options);
        _service = new(_userProvider, _accessTokenIssuer, new DefaultTenantAccessor(), options);
    }

    [Fact]
    public async Task RefreshAsyncRejectsAccessAndTamperedTokens()
    {
        var context = new TokenIssuanceContext(User, [], [], []);
        var accessToken = await _tokenService.IssueAccessTokenAsync(context);
        var refreshToken = (await _tokenService.IssueRefreshTokenAsync(context)).Token;

        // The first character of the signature: all of its bits count. Only some of the last one's do, so changing
        // that one can leave the signature intact.
        var signature = refreshToken.LastIndexOf('.') + 1;
        var tamperedRefreshToken = refreshToken[..signature] + (refreshToken[signature] == 'A' ? 'B' : 'A') + refreshToken[(signature + 1)..];

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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefreshAsyncRejectsATokenWithABlankSubjectEvenWhenTheNameWasReused(string subject)
    {
        _userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Name == User.Name), Arg.Any<CancellationToken>()).Returns(User);

        Assert.Null(await _service.RefreshAsync(LegacyRefreshToken.CreateWithSubject(_tokenOptions, User, subject)));
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncRejectsATokenWithoutASubjectEvenWhenTheNameWasReused()
    {
        _userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Name == User.Name), Arg.Any<CancellationToken>()).Returns(User);

        Assert.Null(await _service.RefreshAsync(LegacyRefreshToken.CreateWithoutSubject(_tokenOptions, User)));
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncRejectsATokenWithANameIdentifierAndABlankSubject()
    {
        Assert.Null(await _service.RefreshAsync(LegacyRefreshToken.CreateWithSubjectClaims(
            _tokenOptions,
            User,
            new Claim(ClaimTypes.NameIdentifier, Victim.Id),
            new Claim(JwtRegisteredClaimNames.Sub, ""))));
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncRejectsATokenWithConflictingNameIdentifierAndSubject()
    {
        Assert.Null(await _service.RefreshAsync(LegacyRefreshToken.CreateWithSubjectClaims(
            _tokenOptions,
            User,
            new Claim(ClaimTypes.NameIdentifier, Victim.Id),
            new Claim(JwtRegisteredClaimNames.Sub, User.Id))));
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncRejectsATokenWithTwoDifferentSubjects()
    {
        Assert.Null(await _service.RefreshAsync(LegacyRefreshToken.CreateWithSubjectClaims(
            _tokenOptions,
            User,
            new Claim(JwtRegisteredClaimNames.Sub, Victim.Id),
            new Claim(JwtRegisteredClaimNames.Sub, User.Id))));
        await _accessTokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsyncAcceptsATokenWithMatchingSubjectAndNameIdentifier()
    {
        Assert.Same(RefreshedTokens, await _service.RefreshAsync(LegacyRefreshToken.CreateWithSubjectClaims(
            _tokenOptions,
            User,
            new Claim(JwtRegisteredClaimNames.Sub, User.Id),
            new Claim(ClaimTypes.NameIdentifier, User.Id))));
    }

    private sealed class CurrentClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}

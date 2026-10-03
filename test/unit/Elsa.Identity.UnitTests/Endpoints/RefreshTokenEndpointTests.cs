using System.Security.Claims;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using RefreshTokenEndpoint = Elsa.Identity.Endpoints.RefreshToken.RefreshToken;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// Refresh-token user resolution on the HTTP endpoint. Main has no SignInSession / logout
/// work from the 3.9 identity PRs, so this exercises the endpoint through FastEndpoints'
/// factory rather than a full authentication host.
/// </summary>
public class RefreshTokenEndpointTests
{
    private static readonly User Alice = new() { Id = "alice-id", Name = "alice" };
    private static readonly IssuedTokens Tokens = new("access-a", "refresh-a");

    [Fact]
    public async Task ANormalRefreshStillWorks()
    {
        var userProvider = Substitute.For<IUserProvider>();
        var tokenIssuer = Substitute.For<IAccessTokenIssuer>();
        userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Id == Alice.Id), Arg.Any<CancellationToken>()).Returns(Alice);
        tokenIssuer.IssueTokensAsync(Alice, Arg.Any<CancellationToken>()).Returns(Tokens);
        var endpoint = CreateEndpoint(userProvider, tokenIssuer, new Claim(ClaimTypes.NameIdentifier, Alice.Id), new Claim(ClaimTypes.Name, Alice.Name));

        await endpoint.HandleAsync(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, endpoint.HttpContext.Response.StatusCode);
        await tokenIssuer.Received(1).IssueTokensAsync(Alice, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ARefreshTokenIsRejectedWhenTheUserIsDeletedAndRecreatedWithTheSameName()
    {
        var replacement = new User { Id = "alice-id-2", Name = Alice.Name };
        var userProvider = Substitute.For<IUserProvider>();
        var tokenIssuer = Substitute.For<IAccessTokenIssuer>();
        userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Id == Alice.Id), Arg.Any<CancellationToken>()).Returns((User?)null);
        userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Name == Alice.Name), Arg.Any<CancellationToken>()).Returns(replacement);
        tokenIssuer.IssueTokensAsync(replacement, Arg.Any<CancellationToken>()).Returns(new IssuedTokens("access-b", "refresh-b"));
        var endpoint = CreateEndpoint(
            userProvider,
            tokenIssuer,
            new Claim(ClaimTypes.NameIdentifier, Alice.Id),
            new Claim(JwtRegisteredClaimNames.Sub, Alice.Id),
            new Claim(ClaimTypes.Name, Alice.Name));

        await endpoint.HandleAsync(CancellationToken.None);

        Assert.Equal(StatusCodes.Status401Unauthorized, endpoint.HttpContext.Response.StatusCode);
        await tokenIssuer.DidNotReceive().IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ALegacyTokenWithoutSubjectStillResolvesByName()
    {
        var userProvider = Substitute.For<IUserProvider>();
        var tokenIssuer = Substitute.For<IAccessTokenIssuer>();
        userProvider.FindAsync(Arg.Is<UserFilter>(x => x.Name == Alice.Name), Arg.Any<CancellationToken>()).Returns(Alice);
        tokenIssuer.IssueTokensAsync(Alice, Arg.Any<CancellationToken>()).Returns(Tokens);
        var endpoint = CreateEndpoint(userProvider, tokenIssuer, new Claim(ClaimTypes.Name, Alice.Name));

        await endpoint.HandleAsync(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, endpoint.HttpContext.Response.StatusCode);
        await tokenIssuer.Received(1).IssueTokensAsync(Alice, Arg.Any<CancellationToken>());
        await userProvider.DidNotReceive().FindAsync(Arg.Is<UserFilter>(x => x.Id != null), Arg.Any<CancellationToken>());
    }

    private static RefreshTokenEndpoint CreateEndpoint(IUserProvider userProvider, IAccessTokenIssuer tokenIssuer, params Claim[] claims)
    {
        return Factory.Create<RefreshTokenEndpoint>(context =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "refresh"));
        }, userProvider, tokenIssuer);
    }
}

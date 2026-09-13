using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Compatibility;

/// <summary>
/// Protects the existing direct local-credential contracts while the broker-local flow remains additive.
/// </summary>
public sealed class LegacyIdentityEndpointTests : WebApplicationTest<LegacyIdentityEndpointWebApplicationFactory, ExternalAuthenticationTestEntryPoint>
{
    private readonly IUserCredentialsValidator _credentialsValidator = Substitute.For<IUserCredentialsValidator>();
    private readonly IUserProvider _userProvider = Substitute.For<IUserProvider>();
    private readonly IAccessTokenIssuer _tokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly TestAuthenticationState _authentication = new();
    private HttpClient? _client;

    private HttpClient Client => _client ??= Factory.CreateClient();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        _authentication.SetClaims(new Claim(ClaimTypes.Name, "admin"));
        services.AddSingleton(_authentication);
        services.AddSingleton(_credentialsValidator);
        services.AddSingleton(_userProvider);
        services.AddSingleton(_tokenIssuer);
    }

    [Test]
    public async Task IdentityLoginRetainsItsRouteCredentialValidationAndResponseShape()
    {
        var user = new User { Id = "user-a", Name = "admin" };
        _credentialsValidator.ValidateAsync("admin", "password", Arg.Any<CancellationToken>()).Returns(user);
        _tokenIssuer.IssueTokensAsync(user, Arg.Any<CancellationToken>()).Returns(new IssuedTokens("access-a", "refresh-a"));

        var response = await Client.PostAsJsonAsync("/identity/login", new { username = " admin ", password = " password " });
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(document.RootElement.GetProperty("isAuthenticated").GetBoolean()).IsTrue();
        await Assert.That(document.RootElement.GetProperty("accessToken").GetString()).IsEqualTo("access-a");
        await Assert.That(document.RootElement.GetProperty("refreshToken").GetString()).IsEqualTo("refresh-a");
        await _credentialsValidator.Received(1).ValidateAsync("admin", "password", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IdentityLoginRetainsItsGenericUnauthenticatedResponse()
    {
        _credentialsValidator.ValidateAsync("unknown", "wrong", Arg.Any<CancellationToken>()).Returns((User?)null);

        var response = await Client.PostAsJsonAsync("/identity/login", new { username = "unknown", password = "wrong" });
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(document.RootElement.GetProperty("isAuthenticated").GetBoolean()).IsFalse();
        await Assert.That(document.RootElement.GetProperty("accessToken").ValueKind).IsEqualTo(JsonValueKind.Null);
        await Assert.That(document.RootElement.GetProperty("refreshToken").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task IdentityRefreshTokenRetainsItsRouteAndLocalTokenContract()
    {
        var user = new User { Id = "user-a", Name = "admin" };
        _userProvider.FindAsync(Arg.Is<UserFilter>(filter => filter.Name == "admin"), Arg.Any<CancellationToken>()).Returns(user);
        _tokenIssuer.IssueTokensAsync(user, Arg.Any<CancellationToken>()).Returns(new IssuedTokens("access-b", "refresh-b"));

        var response = await Client.PostAsync("/identity/refresh-token", null);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(document.RootElement.GetProperty("isAuthenticated").GetBoolean()).IsTrue();
        await Assert.That(document.RootElement.GetProperty("accessToken").GetString()).IsEqualTo("access-b");
        await Assert.That(document.RootElement.GetProperty("refreshToken").GetString()).IsEqualTo("refresh-b");
    }
}

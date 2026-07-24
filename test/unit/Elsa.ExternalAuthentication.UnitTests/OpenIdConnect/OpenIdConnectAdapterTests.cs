using System.Text.Json;
using System.Security.Claims;
using System.Text;
using System.Net;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.OpenIdConnect.Models;
using Elsa.ExternalAuthentication.OpenIdConnect.Services;
using Elsa.ExternalAuthentication.OpenIdConnect.Validation;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.UnitTests.Foundational;
using Elsa.ExternalAuthentication.Validation;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.UnitTests.OpenIdConnect;

public class OpenIdConnectAdapterTests
{
    [Fact]
    public void AcceptsDiscoverySettingsAndRejectsIncompleteManualTrust()
    {
        var parser = new OpenIdConnectSettingsParser();
        var discovery = Parse("""{"mode":"discovery","authority":"https://issuer.example","clientId":"elsa","callbackUri":"https://elsa.example/external-authentication/callback/connection"}""");
        var manual = Parse("""{"mode":"manual","authority":"https://issuer.example","clientId":"elsa","callbackUri":"https://elsa.example/external-authentication/callback/connection","issuer":"https://issuer.example"}""");

        Assert.True(parser.TryParse(discovery, out var discoverySettings, out var discoveryErrors));
        Assert.Equal(OpenIdConnectTrustMode.Discovery, discoverySettings!.TrustMode);
        Assert.Empty(discoveryErrors);

        Assert.False(parser.TryParse(manual, out _, out var manualErrors));
        Assert.Contains(manualErrors, error => error.Field == "tokenEndpoint");
        Assert.Contains(manualErrors, error => error.Field == "signingKeys");
    }

    [Fact]
    public async Task CreatesAuthorizationCodeRequestWithStateNonceAndProviderPkce()
    {
        var adapter = CreateAdapter(new StaticResponseHandler());
        var connection = ExternalAuthenticationTestData.CreateConnection("connection", "*", "contoso");
        connection.AdapterSettingsVersion = 1;
        connection.AdapterSettings = Parse("""{"mode":"manual","authority":"https://issuer.example","issuer":"https://issuer.example","authorizationEndpoint":"https://issuer.example/authorize","tokenEndpoint":"https://issuer.example/token","callbackUri":"https://elsa.example/external-authentication/callback/connection","clientId":"elsa","signingKeys":{"keys":[]},"providerPkce":"required","scopes":["openid","profile"]}""");
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var transaction = new BrokerTransaction { HandleHash = "stored-hash", ProviderNonce = "nonce", PkceChallenge = "client-pkce", CallbackUri = new Uri("https://studio.example/callback"), ClientId = "studio", ReturnPath = "/", TenantId = "tenant-a" };

        var request = await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), transaction, "provider-state", new TestSystemClock(DateTimeOffset.UtcNow)));
        var query = ParseQuery(request.NavigationUri);

        Assert.Equal("code", query["response_type"]);
        Assert.Equal("provider-state", query["state"]);
        Assert.Equal("nonce", query["nonce"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.NotEmpty(query["code_challenge"]);
        Assert.NotEmpty(request.ProtectedAdapterState);
    }

    [Fact]
    public void DescriptorIsVersionedAndDeclaresSecretBindingField()
    {
        var descriptor = CreateAdapter(new StaticResponseHandler()).Describe();

        Assert.Equal(OpenIdConnectExternalAuthenticationAdapter.AdapterType, descriptor.Type);
        Assert.Equal(1, descriptor.SettingsVersion);
        Assert.Contains(descriptor.Fields, field => field.Name == "clientSecret" && field.IsSecretBinding);
        Assert.True(descriptor.Capabilities.SupportsUpstreamLogout);
    }

    [Fact]
    public async Task RejectsProviderCallbackErrorsBeforeAnyTokenProcessing()
    {
        var adapter = CreateAdapter(new StaticResponseHandler());
        var connection = ExternalAuthenticationTestData.CreateConnection("connection", "*", "contoso");
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var transaction = new BrokerTransaction { HandleHash = "state", PkceChallenge = "client-pkce", CallbackUri = new Uri("https://studio.example/callback"), ClientId = "studio", ReturnPath = "/", TenantId = "tenant-a" };
        var parameters = new Dictionary<string, IReadOnlyCollection<string>> { ["error"] = ["access_denied"] };

        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(effective, new Dictionary<string, ResolvedSecretBinding>(), transaction, "state", parameters, new TestSystemClock(DateTimeOffset.UtcNow))).AsTask());
    }

    [Fact]
    public async Task RejectsCallbackWhenTheRawCorrelationStateDoesNotMatch()
    {
        var connection = CreateConnection(CreateManualSettings());
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var parameters = new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = ["unexpected-state"] };
        var adapter = CreateAdapter(new StaticResponseHandler());

        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(effective, new Dictionary<string, ResolvedSecretBinding>(), CreateTransaction(), "provider-state", parameters, new TestSystemClock(DateTimeOffset.UtcNow))).AsTask());
    }

    [Fact]
    public async Task AppliesClaimProjectionBoundsWhileKeepingRedactedClaims()
    {
        var token = CreateToken(new[] { new Claim("name", "Ada"), new Claim("email", "secret"), new Claim("groups", "toolong") });
        var projection = new ClaimProjection(
            new HashSet<string>(StringComparer.Ordinal) { "name", "email", "groups" },
            new HashSet<string>(StringComparer.Ordinal) { "email" },
            2,
            6,
            9);

        var result = await AuthenticateAsync(token, projection);

        Assert.Equal("subject", result.Identity.Subject);
        Assert.Equal(["Ada"], result.ProjectedClaims["name"]);
        Assert.Equal(["secret"], result.ProjectedClaims["email"]);
        Assert.DoesNotContain("groups", result.ProjectedClaims.Keys);
    }

    [Fact]
    public async Task RejectsInvalidSignatureIssuerAudienceAzpExpiryNonceAndMissingCode()
    {
        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => AuthenticateAsync(CreateToken(signingKey: new SymmetricSecurityKey(Encoding.UTF8.GetBytes("different-signing-key-must-be-long")))));
        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => AuthenticateAsync(CreateToken(issuer: "https://other.example")));
        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => AuthenticateAsync(CreateToken(audience: "other-client")));
        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => AuthenticateAsync(CreateToken(expires: DateTimeOffset.UtcNow.AddMinutes(-5))));
        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => AuthenticateAsync(CreateToken(nonce: "other-nonce")));
        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => AuthenticateAsync(CreateToken(), includeCode: false));
        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => AuthenticateAsync(CreateToken(audiences: ["elsa", "another"], azp: "other-client")));
    }

    [Fact]
    public async Task SupportsBothUpstreamPkceModes()
    {
        var required = await CreateAuthorizationRequestAsync("required");
        var disabled = await CreateAuthorizationRequestAsync("disabled");

        Assert.Contains("code_challenge_method=S256", required.NavigationUri.Query);
        Assert.DoesNotContain("code_challenge", disabled.NavigationUri.Query);
    }

    [Theory]
    [InlineData("http://issuer.example")]
    [InlineData("https://issuer.example", "http://issuer.example/authorize")]
    public async Task RejectsNonHttpsDiscoveryMetadata(string issuer, string? authorizationEndpoint = null)
    {
        var metadata = $$"""{"issuer":"{{issuer}}","authorization_endpoint":"{{authorizationEndpoint ?? "https://issuer.example/authorize"}}","token_endpoint":"https://issuer.example/token","jwks_uri":"https://issuer.example/keys"}""";
        var connection = CreateConnection(Parse("""{"mode":"discovery","authority":"https://issuer.example","clientId":"elsa","callbackUri":"https://elsa.example/external-authentication/callback/connection"}"""));
        var adapter = CreateAdapter(new TokenResponseHandler("", metadata));
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var transaction = CreateTransaction();

        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), transaction, "state", new TestSystemClock(DateTimeOffset.UtcNow))).AsTask());
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static OpenIdConnectExternalAuthenticationAdapter CreateAdapter(HttpMessageHandler handler)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions());
        var validator = new OutboundDestinationValidator(options, new StaticPublicDnsResolver());
        return new OpenIdConnectExternalAuthenticationAdapter(new ProviderHttpClient(new HttpMessageInvoker(handler), validator, options), new OpenIdConnectSettingsParser());
    }

    private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes("test-signing-key-must-be-at-least-32-bytes")) { KeyId = "test-key" };

    private static async Task<ExternalAuthenticationResult> AuthenticateAsync(string token, ClaimProjection? projection = null, bool includeCode = true)
    {
        var connection = CreateConnection(CreateManualSettings());
        connection.ClaimProjection = projection ?? new ClaimProjection(new HashSet<string>(StringComparer.Ordinal) { "name" }, new HashSet<string>(), 8, 128, 1024);
        var adapter = CreateAdapter(new TokenResponseHandler(token));
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var parameters = new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = ["provider-state"] };
        if (includeCode)
            parameters["code"] = ["provider-code"];
        return await adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(effective, new Dictionary<string, ResolvedSecretBinding>(), CreateTransaction(), "provider-state", parameters, new TestSystemClock(DateTimeOffset.UtcNow)));
    }

    private static async Task<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(string pkce)
    {
        var connection = CreateConnection(CreateManualSettings(pkce));
        var adapter = CreateAdapter(new StaticResponseHandler());
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        return await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), CreateTransaction(), "provider-state", new TestSystemClock(DateTimeOffset.UtcNow)));
    }

    private static IdentityProviderConnection CreateConnection(JsonElement settings)
    {
        var connection = ExternalAuthenticationTestData.CreateConnection("connection", "*", "contoso");
        connection.AdapterSettingsVersion = 1;
        connection.AdapterSettings = settings;
        return connection;
    }

    private static BrokerTransaction CreateTransaction() => new() { HandleHash = "stored-hash", ProviderNonce = "nonce", PkceChallenge = "client-pkce", CallbackUri = new Uri("https://studio.example/callback"), ClientId = "studio", ReturnPath = "/", TenantId = "tenant-a" };

    private static JsonElement CreateManualSettings(string pkce = "disabled") => Parse($$"""{"mode":"manual","authority":"https://issuer.example","issuer":"https://issuer.example","authorizationEndpoint":"https://issuer.example/authorize","tokenEndpoint":"https://issuer.example/token","callbackUri":"https://elsa.example/external-authentication/callback/connection","clientId":"elsa","signingKeys":{"keys":[{"kty":"oct","kid":"test-key","k":"{{Base64UrlEncoder.Encode(SigningKey.Key)}}"}]},"providerPkce":"{{pkce}}"}""");

    private static string CreateToken(IEnumerable<Claim>? claims = null, SymmetricSecurityKey? signingKey = null, string issuer = "https://issuer.example", string audience = "elsa", string nonce = "nonce", DateTimeOffset? expires = null, IReadOnlyCollection<string>? audiences = null, string? azp = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audiences is null ? audience : null,
            Subject = new ClaimsIdentity([new Claim("sub", "subject"), new Claim("nonce", nonce), .. (claims ?? [])]),
            Expires = (expires ?? DateTimeOffset.UtcNow.AddMinutes(5)).UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey ?? SigningKey, SecurityAlgorithms.HmacSha256)
        };
        if (audiences is not null)
            descriptor.Claims = new Dictionary<string, object> { ["aud"] = audiences.ToArray(), ["azp"] = azp ?? "elsa" };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(Uri uri) => uri.Query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .ToDictionary(parts => Uri.UnescapeDataString(parts[0]), parts => parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty, StringComparer.Ordinal);

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private sealed class TokenResponseHandler(string token, string? discovery = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get && discovery is not null)
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(discovery) });

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent($"{{\"id_token\":\"{token}\"}}") });
        }
    }

    private sealed class StaticPublicDnsResolver : IOutboundDnsResolver
    {
        private static readonly IReadOnlyCollection<IPAddress> Addresses = [IPAddress.Parse("8.8.8.8")];

        public ValueTask<IReadOnlyCollection<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) => ValueTask.FromResult(Addresses);
    }
}

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
        var discovery = Parse("""{"mode":"discovery","discoveryUrl":"https://issuer.example/.well-known/openid-configuration","clientId":"elsa"}""");
        var manual = Parse("""{"mode":"manual","clientId":"elsa","issuer":"https://issuer.example"}""");

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
        connection.AdapterSettingsVersion = 2;
        connection.AdapterSettings = Parse("""{"mode":"manual","issuer":"https://issuer.example","authorizationEndpoint":"https://issuer.example/authorize","tokenEndpoint":"https://issuer.example/token","clientId":"elsa","clientAuthenticationMethod":"client_secret_basic","signingKeys":{"keys":[]},"scopes":["openid","profile"]}""");
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
        Assert.Equal(2, descriptor.SettingsVersion);
        Assert.Contains(descriptor.Fields, field => field.Name == "clientSecret" && field.IsSecretBinding);
        Assert.True(descriptor.Capabilities.SupportsUpstreamLogout);
    }

    [Fact]
    public async Task ClientSecretBasicEncodesReservedCredentialsAndOmitsThemFromTheFormBody()
    {
        var handler = new CapturingTokenResponseHandler(CreateToken(audience: "client:id"));
        var adapter = CreateAdapter(handler);
        var connection = CreateConnection(JsonSerializer.SerializeToElement(new
        {
            mode = "manual",
            clientId = "client:id",
            clientAuthenticationMethod = "client_secret_basic",
            issuer = "https://issuer.example",
            authorizationEndpoint = "https://issuer.example/authorize",
            tokenEndpoint = "https://issuer.example/token",
            signingKeys = new { keys = new[] { new { kty = "oct", kid = "test-key", k = Base64UrlEncoder.Encode(SigningKey.Key) } } }
        }));
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var secrets = new Dictionary<string, ResolvedSecretBinding> { ["clientSecret"] = new(new SensitiveString("secret +/&"), "generation") };

        try
        {
            await adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(effective, secrets, CreateTransaction(), "provider-state", new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = ["provider-state"], ["code"] = ["provider-code"] }, new TestSystemClock(DateTimeOffset.UtcNow)));

            Assert.Equal("Basic Y2xpZW50JTNBaWQ6c2VjcmV0KyUyQiUyRiUyNg==", handler.Authorization);
            Assert.DoesNotContain("client_id", handler.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("client_secret", handler.Body, StringComparison.Ordinal);
        }
        finally
        {
            secrets["clientSecret"].Value.Dispose();
        }
    }

    [Fact]
    public async Task ValidationRejectsMissingDeploymentCallbackBaseUri()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions());
        var adapter = new OpenIdConnectExternalAuthenticationAdapter(
            new ProviderHttpClient(new HttpMessageInvoker(new StaticResponseHandler()), new OutboundDestinationValidator(options, new StaticPublicDnsResolver()), options),
            new OpenIdConnectSettingsParser(),
            options);
        var connection = CreateConnection(CreateManualSettings());
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");

        var result = await adapter.ValidateAsync(new ConnectionValidationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), new TestSystemClock(DateTimeOffset.UtcNow)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == "redirects.externalCallbackBaseUri");
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
    public async Task AlwaysUsesS256UpstreamPkce()
    {
        var request = await CreateAuthorizationRequestAsync();

        Assert.Contains("code_challenge_method=S256", request.NavigationUri.Query);
        Assert.Contains("code_challenge=", request.NavigationUri.Query);
    }

    [Theory]
    [InlineData("https://elsa.example/gateway")]
    [InlineData("https://elsa.example/gateway/")]
    public async Task UsesPurposeSpecificCallbacksWithoutDiscardingTheDeploymentBasePath(string callbackBaseUri)
    {
        var adapter = CreateAdapter(new StaticResponseHandler(), new Uri(callbackBaseUri));
        var connection = CreateConnection(CreateManualSettings());
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var clock = new TestSystemClock(DateTimeOffset.UtcNow);
        var signIn = await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), CreateTransaction(), "sign-in-state", clock));
        var previewTransaction = CreateTransaction();
        previewTransaction.Purpose = BrokerTransactionPurpose.Preview;
        var preview = await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), previewTransaction, "preview-state", clock));
        var logout = await adapter.CreateLogoutRequestAsync(new ExternalLogoutContext(effective, new Dictionary<string, ResolvedSecretBinding>(), new BrokerTransaction { Purpose = BrokerTransactionPurpose.UpstreamLogout }, "logout-state", clock));

        Assert.Equal("https://elsa.example/gateway/external-authentication/callback/contoso", ParseQuery(signIn.NavigationUri)["redirect_uri"]);
        Assert.Equal("https://elsa.example/gateway/external-authentication/previews/callback/connection", ParseQuery(preview.NavigationUri)["redirect_uri"]);
        Assert.Equal("https://elsa.example/gateway/external-authentication/logout/callback/contoso", ParseQuery(Assert.IsType<ExternalLogoutRequest>(logout).NavigationUri)["post_logout_redirect_uri"]);
    }

    [Fact]
    public async Task UsesThePreviewCallbackAgainForTheTokenExchange()
    {
        var handler = new CapturingTokenResponseHandler(CreateToken());
        var adapter = CreateAdapter(handler);
        var connection = CreateConnection(CreateManualSettings());
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var transaction = CreateTransaction();
        transaction.Purpose = BrokerTransactionPurpose.Preview;
        var authorization = await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), transaction, "provider-state", new TestSystemClock(DateTimeOffset.UtcNow)));
        transaction.ProtectedPayload = authorization.ProtectedAdapterState;
        var secret = new SensitiveString("secret");

        try
        {
            await adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(effective, new Dictionary<string, ResolvedSecretBinding> { ["clientSecret"] = new(secret, "generation") }, transaction, "provider-state", new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = ["provider-state"], ["code"] = ["provider-code"] }, new TestSystemClock(DateTimeOffset.UtcNow)));

            Assert.Contains("redirect_uri=https%3A%2F%2Felsa.example%2Fexternal-authentication%2Fpreviews%2Fcallback%2Fconnection", handler.Body, StringComparison.Ordinal);
        }
        finally
        {
            secret.Dispose();
        }
    }

    [Theory]
    [InlineData("http://issuer.example")]
    [InlineData("https://issuer.example", "http://issuer.example/authorize")]
    public async Task RejectsNonHttpsDiscoveryMetadata(string issuer, string? authorizationEndpoint = null)
    {
        var metadata = $$"""{"issuer":"{{issuer}}","authorization_endpoint":"{{authorizationEndpoint ?? "https://issuer.example/authorize"}}","token_endpoint":"https://issuer.example/token","jwks_uri":"https://issuer.example/keys"}""";
        var connection = CreateConnection(Parse("""{"mode":"discovery","discoveryUrl":"https://issuer.example/.well-known/openid-configuration","clientId":"elsa","clientAuthenticationMethod":"client_secret_basic"}"""));
        var adapter = CreateAdapter(new TokenResponseHandler("", metadata));
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        var transaction = CreateTransaction();

        await Assert.ThrowsAsync<OpenIdConnectAuthenticationException>(() => adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), transaction, "state", new TestSystemClock(DateTimeOffset.UtcNow))).AsTask());
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static OpenIdConnectExternalAuthenticationAdapter CreateAdapter(HttpMessageHandler handler, Uri? externalCallbackBaseUri = null)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions { Redirects = new RedirectValidationOptions { ExternalCallbackBaseUri = externalCallbackBaseUri ?? new Uri("https://elsa.example/") } });
        var validator = new OutboundDestinationValidator(options, new StaticPublicDnsResolver());
        return new OpenIdConnectExternalAuthenticationAdapter(new ProviderHttpClient(new HttpMessageInvoker(handler), validator, options), new OpenIdConnectSettingsParser(), options);
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
        var secret = new SensitiveString("test-secret");
        try
        {
            var secrets = new Dictionary<string, ResolvedSecretBinding> { ["clientSecret"] = new(secret, "generation") };
            return await adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(effective, secrets, CreateTransaction(), "provider-state", parameters, new TestSystemClock(DateTimeOffset.UtcNow)));
        }
        finally
        {
            secret.Dispose();
        }
    }

    private static async Task<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync()
    {
        var connection = CreateConnection(CreateManualSettings());
        var adapter = CreateAdapter(new StaticResponseHandler());
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "configuration");
        return await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), CreateTransaction(), "provider-state", new TestSystemClock(DateTimeOffset.UtcNow)));
    }

    private static IdentityProviderConnection CreateConnection(JsonElement settings)
    {
        var connection = ExternalAuthenticationTestData.CreateConnection("connection", "*", "contoso");
        connection.AdapterSettingsVersion = 2;
        connection.AdapterSettings = settings;
        return connection;
    }

    private static BrokerTransaction CreateTransaction() => new() { HandleHash = "stored-hash", ProviderNonce = "nonce", PkceChallenge = "client-pkce", CallbackUri = new Uri("https://studio.example/callback"), ClientId = "studio", ReturnPath = "/", TenantId = "tenant-a" };

    private static JsonElement CreateManualSettings() => JsonSerializer.SerializeToElement(new
    {
        mode = "manual",
        issuer = "https://issuer.example",
        authorizationEndpoint = "https://issuer.example/authorize",
        tokenEndpoint = "https://issuer.example/token",
        endSessionEndpoint = "https://issuer.example/logout",
        clientId = "elsa",
        clientAuthenticationMethod = "client_secret_basic",
        signingKeys = new
        {
            keys = new[]
            {
                new { kty = "oct", kid = "test-key", k = Base64UrlEncoder.Encode(SigningKey.Key) }
            }
        }
    });

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

    private sealed class CapturingTokenResponseHandler(string token) : HttpMessageHandler
    {
        public string? Authorization { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{{\"id_token\":\"{token}\"}}") };
        }
    }

    private sealed class StaticPublicDnsResolver : IOutboundDnsResolver
    {
        private static readonly IReadOnlyCollection<IPAddress> Addresses = [IPAddress.Parse("8.8.8.8")];

        public ValueTask<IReadOnlyCollection<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) => ValueTask.FromResult(Addresses);
    }
}

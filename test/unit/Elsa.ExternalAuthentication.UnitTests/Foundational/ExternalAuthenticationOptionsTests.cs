using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Elsa.Extensions;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationOptionsTests
{
    [Test]
    public async Task BindsAuthenticationClientsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalAuthentication:AuthenticationClients:0:ClientId"] = "elsa-studio-server",
                ["ExternalAuthentication:AuthenticationClients:0:DisplayName"] = "Elsa Studio Server",
                ["ExternalAuthentication:AuthenticationClients:0:ClientType"] = "Confidential",
                ["ExternalAuthentication:AuthenticationClients:0:CallbackUris:0"] = "https://localhost:7113/authentication/external/callback",
                ["ExternalAuthentication:AuthenticationClients:0:LogoutCallbackUris:0"] = "https://localhost:7113/authentication/external/logout-callback",
                ["ExternalAuthentication:AuthenticationClients:0:AllowedReturnPathPrefixes:0"] = "/",
                ["ExternalAuthentication:AuthenticationClients:0:SecretBinding:Ownership"] = "External",
                ["ExternalAuthentication:AuthenticationClients:0:SecretBinding:ResolverType"] = "configuration",
                ["ExternalAuthentication:AuthenticationClients:0:SecretBinding:Reference"] = "ExternalAuthentication:Secrets:StudioServerClientSecret",
                ["ExternalAuthentication:AuthenticationClients:0:IsEnabled"] = "true"
            })
            .Build();
        var options = new ExternalAuthenticationOptions();

        configuration.GetSection("ExternalAuthentication").Bind(options);

        var client = await Assert.That(options.Clients).HasSingleItem();
        await Assert.That(client.ClientId).IsEqualTo("elsa-studio-server");
        await Assert.That(client.ClientType).IsEqualTo(AuthenticationClientType.Confidential);
        await Assert.That(client.CallbackUris).Contains(new Uri("https://localhost:7113/authentication/external/callback"));
        await Assert.That(client.LogoutCallbackUris).Contains(new Uri("https://localhost:7113/authentication/external/logout-callback"));
        await Assert.That(client.AllowedReturnPathPrefixes).Contains("/");
        await Assert.That(client.SecretBinding?.ResolverType).IsEqualTo("configuration");
        await Assert.That(client.SecretBinding?.Reference).IsEqualTo("ExternalAuthentication:Secrets:StudioServerClientSecret");
        await Assert.That(client.IsEnabled).IsTrue();
    }

    [Test]
    public async Task BindsConnectionJsonSettingsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalAuthentication:Connections:0:Key"] = "keycloak",
                ["ExternalAuthentication:Connections:0:AdapterType"] = "openid-connect",
                ["ExternalAuthentication:Connections:0:AdapterSettings:mode"] = "discovery",
                ["ExternalAuthentication:Connections:0:AdapterSettings:discoveryUrl"] = "https://localhost:8443/realms/elsa/.well-known/openid-configuration",
                ["ExternalAuthentication:Connections:0:AdapterSettings:clientId"] = "elsa-studio-idp",
                ["ExternalAuthentication:Connections:0:AdapterSettings:scopes:0"] = "profile",
                ["ExternalAuthentication:Connections:0:AdapterSettings:scopes:1"] = "email",
                ["ExternalAuthentication:Connections:0:UnlinkedPolicy:Type"] = "create-user",
                ["ExternalAuthentication:Connections:0:UnlinkedPolicy:SettingsVersion"] = "1",
                ["ExternalAuthentication:Connections:0:UnlinkedPolicy:Settings:defaultRoleIds:0"] = "admin",
                ["ExternalAuthentication:Connections:0:PermissionGrantSources:0:Type"] = "claim-mapping",
                ["ExternalAuthentication:Connections:0:PermissionGrantSources:0:SettingsVersion"] = "1",
                ["ExternalAuthentication:Connections:0:PermissionGrantSources:0:Order"] = "10",
                ["ExternalAuthentication:Connections:0:PermissionGrantSources:0:Settings:claimType"] = "groups"
            })
            .Build();
        var options = new ExternalAuthenticationOptions();

        configuration.GetSection("ExternalAuthentication").BindExternalAuthenticationOptions(options);

        var connection = await Assert.That(options.ConfigurationConnections).HasSingleItem();
        await Assert.That(connection.AdapterSettings.GetProperty("mode").GetString()).IsEqualTo("discovery");
        await Assert.That(connection.AdapterSettings.GetProperty("discoveryUrl").GetString()).IsEqualTo("https://localhost:8443/realms/elsa/.well-known/openid-configuration");
        await Assert.That(connection.AdapterSettings.GetProperty("clientId").GetString()).IsEqualTo("elsa-studio-idp");
        string?[] expectedScopes = ["profile", "email"];
        await Assert.That(connection.AdapterSettings.GetProperty("scopes").EnumerateArray().Select(x => x.GetString())).IsEquivalentTo(expectedScopes, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(connection.UnlinkedPolicy?.Settings.GetProperty("defaultRoleIds")[0].GetString()).IsEqualTo("admin");
        var grantSource = await Assert.That(connection.PermissionGrantSources).HasSingleItem();
        await Assert.That(grantSource.Settings.GetProperty("claimType").GetString()).IsEqualTo("groups");
    }

    [Test]
    public async Task DefaultsFavorTheMostRestrictiveOperationalSettings()
    {
        var options = new ExternalAuthenticationOptions();

        await Assert.That(options.EnableDatabaseConnections).IsTrue();
        await Assert.That(options.Lifetimes.BrokerTransactionLifetime).IsEqualTo(TimeSpan.FromMinutes(10));
        await Assert.That(options.Lifetimes.CompletionCodeLifetime).IsEqualTo(TimeSpan.FromMinutes(1));
        await Assert.That(options.Claims.MaximumClaimCount).IsEqualTo(64);
        await Assert.That(options.Claims.MaximumValueLength).IsEqualTo(1_024);
        await Assert.That(options.Claims.MaximumTotalBytes).IsEqualTo(16 * 1_024);
        await Assert.That(options.ProviderEgress.RequireHttps).IsTrue();
        await Assert.That(options.ProviderEgress.AllowPrivateNetworkDestinations).IsFalse();
        await Assert.That(options.ProviderEgress.MaximumRedirects).IsEqualTo(3);
        await Assert.That(options.Redirects.RequirePkceS256).IsTrue();
        await Assert.That(options.Redirects.AllowDevelopmentLoopbackCallbacks).IsFalse();
        await Assert.That(options.WebAssemblyPersistence.Persistence).IsEqualTo(BrowserCredentialPersistence.Memory);
        await Assert.That(options.WebAssemblyPersistence.RequireExplicitPersistentStorageWarning).IsTrue();
        await Assert.That(options.FinalLoginPathGuard.IsEnabled).IsTrue();
        await Assert.That(options.FinalLoginPathGuard.RequireRecoveryMethod).IsTrue();
    }

    [Test]
    public async Task ValidatorRejectsDuplicateOrUnavailableExtensionTypes()
    {
        var options = new ExternalAuthenticationOptions
        {
            AllowedAdapterTypes = ["oidc", "oidc", "saml"],
            AllowedUnlinkedIdentityPolicyTypes = ["reject"],
            AllowedPermissionGrantSourceTypes = ["elsa-roles"]
        };
        var validator = CreateValidator(adapters: [new TestAdapter("oidc"), new TestAdapter("oidc")]);

        var result = validator.Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("adapter type 'oidc' is registered more than once", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("allowed adapter type 'oidc' is configured more than once", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("allowed adapter type 'saml' is not installed", StringComparison.Ordinal));
    }

    [Test]
    public async Task ValidatorRejectsInsecureClientRegistrations()
    {
        var options = CreateValidOptions();
        options.Clients =
        [
            new AuthenticationClient(
                "studio",
                "Studio",
                AuthenticationClientType.Public,
                new HashSet<Uri> { new("http://studio.example/callback") },
                new HashSet<Uri>(),
                new HashSet<string> { "https://studio.example/" },
                new HashSet<string>(),
                new SecretBinding("test", "must-not-be-set"),
                true)
        ];

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("invalid callback URI", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("must not define a client secret binding", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("must not contain a path, query, or trailing slash", StringComparison.Ordinal));
    }

    [Test]
    public async Task ValidatorRejectsUnsafeProviderEgressConfiguration()
    {
        var options = CreateValidOptions();
        options.ProviderEgress = new ProviderEgressOptions
        {
            MaximumRedirects = -1,
            ConnectTimeout = TimeSpan.Zero,
            RequestTimeout = TimeSpan.Zero,
            MaximumDiscoveryResponseBytes = 0,
            MaximumTokenResponseBytes = 0,
            MaximumUserInfoResponseBytes = 0,
            AllowedHosts = ["*.example.test"],
            ProxyUri = new Uri("ftp://user:password@proxy.example")
        };

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("egress maximum redirects", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("egress timeouts", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("response-size limits", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("allowed host", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("proxy URI", StringComparison.Ordinal));
    }

    [Test]
    public async Task ValidatorRejectsConfigurationCollisionsAndUnknownAdapterTypes()
    {
        var options = CreateValidOptions();
        var unknownAdapter = ExternalAuthenticationTestData.CreateConnection("unknown", "tenant-b", "partner");
        unknownAdapter.AdapterType = "saml";
        options.ConfigurationConnections =
        [
            ExternalAuthenticationTestData.CreateConnection("host", ConnectionScope.HostTenantId, "corp"),
            ExternalAuthenticationTestData.CreateConnection("tenant", "tenant-a", " CORP "),
            unknownAdapter
        ];

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("must use the host scope", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("configured more than once", StringComparison.Ordinal));
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("selects adapter type 'saml', which is not installed", StringComparison.Ordinal));
    }

    private static ExternalAuthenticationOptions CreateValidOptions() => new()
    {
        AllowedAdapterTypes = ["oidc"],
        AllowedUnlinkedIdentityPolicyTypes = ["reject"],
        AllowedPermissionGrantSourceTypes = ["elsa-roles"]
    };

    private static ExternalAuthenticationOptionsValidator CreateValidator(
        IEnumerable<IExternalAuthenticationAdapter>? adapters = null)
    {
        var extensions = new ExternalAuthenticationExtensionOptions();
        foreach (var adapter in adapters ?? [new TestAdapter("oidc")])
            extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.Adapter, adapter.Type));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, "reject"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "elsa-roles"));
        return new(Microsoft.Extensions.Options.Options.Create(extensions), NullLogger<ExternalAuthenticationOptionsValidator>.Instance);
    }

    private sealed class TestAdapter(string type) : IExternalAuthenticationAdapter
    {
        public string Type => type;
        public ExternalAuthenticationAdapterDescriptor Describe() => throw new NotSupportedException();
        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestUnlinkedIdentityPolicy(string type) : IUnlinkedIdentityPolicy
    {
        public string Type => type;
        public UnlinkedIdentityPolicyDescriptor Describe() => throw new NotSupportedException();
        public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestPermissionGrantSource(string type) : IPermissionGrantSource
    {
        public string Type => type;
        public PermissionGrantSourceDescriptor Describe() => throw new NotSupportedException();
        public ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
